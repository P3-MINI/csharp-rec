namespace P3RecorderStatus.Models;

public class SshConnection
{
    public required string User { get; set; }
    public required string Password { get; set; }
    public required string Host { get; set; }
}