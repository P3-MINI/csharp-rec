using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using P3RecorderStatus.Models;

namespace P3RecorderStatus.ViewModels;

public partial class SshConnectionViewModel(SshConnection sshConnection) : ViewModelBase
{
    [ObservableProperty]
    private string _user = sshConnection.User;

    [ObservableProperty]
    private string _password = sshConnection.Password;
    
    [ObservableProperty]
    private string _host = sshConnection.Host;

    public SshConnection SshConnection => new SshConnection { User = User,  Password = Password, Host = Host };

    public event EventHandler? ConnectionDetailsChanged;

    partial void OnUserChanged(string value)
    {
        ConnectionDetailsChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnPasswordChanged(string value)
    {
        ConnectionDetailsChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnHostChanged(string value)
    {
        ConnectionDetailsChanged?.Invoke(this, EventArgs.Empty);
    }
}