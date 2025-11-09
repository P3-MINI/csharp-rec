using System;
using System.Threading;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using P3RecorderStatus.Models;

namespace P3RecorderStatus.ViewModels;

public partial class StudentViewModel : ViewModelBase, IDisposable
{
    [ObservableProperty]
    private string _userName;
    
    [ObservableProperty]
    private string _name;
    
    [ObservableProperty]
    private string _surname;
    
    [ObservableProperty]
    private string _groups;

    [ObservableProperty]
    private int _album;
    
    [ObservableProperty]
    private DateTime _lastUpdate;

    public string Status => DateTime.Now - LastUpdate < TimeSpan.FromMinutes(2) ? "Online" : "Offline";
    public IBrush StatusColor => Status == "Online" ? Brushes.LawnGreen : Brushes.Red;

    public Student Student => new() { UserName = UserName, LastUpdate = LastUpdate, Name = Name, Surname = Surname, Groups = Groups, Album = Album};

    private readonly Timer? _timer;

    public StudentViewModel(Student student)
    {
        _userName = student.UserName;
        _lastUpdate = student.LastUpdate;
        _name = student.Name;
        _surname = student.Surname;
        _groups = student.Groups;
        _album = student.Album;
        _timer = new Timer(_ =>
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusColor));
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _timer?.Dispose();
    }
}