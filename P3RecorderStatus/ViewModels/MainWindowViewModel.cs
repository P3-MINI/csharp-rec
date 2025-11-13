using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CsvHelper;
using CsvHelper.Configuration;
using P3RecorderStatus.Models;
using P3RecorderStatus.ViewModels.Messages;
using Renci.SshNet;

namespace P3RecorderStatus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private DateTime? _lastUpdated;
    
    public SshConnectionViewModel SshConnection { get; } = new(new SshConnection { Host = "", User = "", Password = "" });
    
    public ObservableCollection<StudentViewModel> Students { get; } = [];
    private List<StudentFilter>? StudentFilters { get; set; }

    private const string StudentsDirectory = "/home2/samba/spychalam/recordings";

    private Timer? _fetchTimer;
    private CancellationTokenSource _tokenSource = new();

    public MainWindowViewModel()
    {
        SshConnection.ConnectionDetailsChanged += async (_, _) =>
        {
            await _tokenSource.CancelAsync();
            _tokenSource = new CancellationTokenSource();
            _fetchTimer?.Dispose();
            StartFetching();
        };
        StartFetching();
    }

    private void StartFetching()
    {
        _fetchTimer = new Timer(async void (_) => await FetchStudentsAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
    }

    [RelayCommand]
    private async Task LoadStudents()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (desktop.MainWindow != null)
        {
            var result = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open CSV file",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }]
            });

            if (result.Count == 0)
            {
                return;
            }

            var file = result[0];
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true
            };
            using var csv = new CsvReader(reader, config);
            csv.Context.RegisterClassMap<UserFilterMap>();
            StudentFilters = csv.GetRecords<StudentFilter>().ToList();
        }

        await FetchStudentsAsync();
    }

    private async Task FetchStudentsAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(SshConnection.Host) || StudentFilters == null)
            {
                return;
            }
            Console.WriteLine($"Attempting to connect to {SshConnection.Host}...");
            using var client = new SftpClient(SshConnection.Host, SshConnection.User, SshConnection.Password);
            client.HostKeyReceived += (_, e) =>
            {
                Console.WriteLine("Host key received. Trusting.");
                e.CanTrust = true;
            };
            await client.ConnectAsync(_tokenSource.Token);
            Console.WriteLine("Successfully connected.");
            var files = client.ListDirectoryAsync(StudentsDirectory, _tokenSource.Token);
            var studentTasks = await files
                .Where(f => f is { IsDirectory: false, Length: > 0, Name: ['P', '3', '_', .., '.', 't', 's'] })
                .Select(f => new { Name = f.Name.Split('_')[1], f.LastWriteTime })
                .GroupBy(entry => entry.Name)
                .Select(async group =>
                {
                    return new
                    {
                        UserName = group.Key,
                        LastUpdate = await group.MaxAsync(entry => entry.LastWriteTime, _tokenSource.Token)
                    };
                })
                .ToListAsync(_tokenSource.Token);

            var students = StudentFilters
                .GroupJoin(await Task.WhenAll(studentTasks),
                    entry => entry.UserName,
                    filter => filter.UserName,
                    (filter, entry) =>
                    {
                        var entries = entry.ToList();
                        return new Student
                        {
                            UserName = filter.UserName,
                            Name = filter.Names,
                            Surname = filter.Surname,
                            Groups = filter.Groups,
                            Album = filter.AlbumNumber,
                            LastUpdate = entries.Count > 0 ? entries[0].LastUpdate : DateTime.MinValue
                        };
                    });

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                var existingStudents = Students.ToDictionary(s => s.UserName);
                var fetchedStudents = students.ToDictionary(s => s.UserName);

                foreach (var fetchedStudent in fetchedStudents.Values)
                {
                    if (existingStudents.TryGetValue(fetchedStudent.UserName, out var existingStudent))
                    {
                        existingStudent.LastUpdate = fetchedStudent.LastUpdate;
                        existingStudent.Name = fetchedStudent.Name;
                        existingStudent.Surname = fetchedStudent.Surname;
                        existingStudent.Groups = fetchedStudent.Groups;
                    }
                    else
                    {
                        Students.Add(new StudentViewModel(fetchedStudent));
                    }
                }

                var studentsToRemove = existingStudents.Keys.Except(fetchedStudents.Keys).ToList();
                foreach (var userNameToRemove in studentsToRemove)
                {
                    if (existingStudents.TryGetValue(userNameToRemove, out var studentToRemove))
                    {
                        studentToRemove.Dispose();
                        Students.Remove(studentToRemove);
                    }
                }

                LastUpdated = DateTime.Now;
                WeakReferenceMessenger.Default.Send(new LastUpdatedChangedMessage(LastUpdated));
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to fetch: {e}");
        }
    }
}

public sealed class UserFilterMap : ClassMap<StudentFilter>
{
    public UserFilterMap()
    {
        Map(m => m.UserName).Name("login");
        Map(m => m.Surname).Name("nazwisko");
        Map(m => m.Names).Name("imiona");
        Map(m => m.Groups).Name("grupy");
        Map(m => m.AlbumNumber).Name("nr albumu");
        Map(m => m.Active).Name("aktywny");
    }
}