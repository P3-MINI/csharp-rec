using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using P3RecorderStatus.Models;
using P3RecorderStatus.ViewModels.Messages;

namespace P3RecorderStatus.ViewModels;

public partial class StudentViewModel : ViewModelBase, IDisposable, IRecipient<LastUpdatedChangedMessage>
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
    [NotifyPropertyChangedFor(nameof(Status))]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    private DateTime _lastUpdate;

    private DateTime? _mainWindowLastUpdate;

    public string Status => (_mainWindowLastUpdate ?? DateTime.MinValue) - LastUpdate < TimeSpan.FromSeconds(20) ? "Online" : "Offline";
    public IBrush StatusColor => Status == "Online" ? Brushes.LawnGreen : Brushes.Red;

    public Student Student => new() { UserName = UserName, LastUpdate = LastUpdate, Name = Name, Surname = Surname, Groups = Groups, Album = Album};

    public StudentViewModel(Student student)
    {
        _userName = student.UserName;
        _lastUpdate = student.LastUpdate;
        _name = student.Name;
        _surname = student.Surname;
        _groups = student.Groups;
        _album = student.Album;
        WeakReferenceMessenger.Default.Register(this);
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    public void Receive(LastUpdatedChangedMessage message)
    {
        _mainWindowLastUpdate = message.Value;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusColor));
    }
}