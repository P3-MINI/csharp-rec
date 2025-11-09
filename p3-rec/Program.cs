using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

var recordingMessage = new FigletText("Recording in progress")
    .Centered()
    .Color(Color.Green);

var ffmpeg = StartFFmpeg();

AnsiConsole.Live(recordingMessage)
    .Start(ctx =>
    {
        Console.CancelKeyPress += (_, e) =>
        {
            AnsiConsole.MarkupLine("[bold red]Stopping recording...[/]");
            ffmpeg.Kill(true);
            e.Cancel = true;
        };

        while (!ffmpeg.HasExited)
        {
            ctx.Refresh();
            Thread.Sleep(500);
        }
    });

AnsiConsole.MarkupLine("[bold green]Recording finished![/]");

Process StartFFmpeg()
{
    string user = Environment.UserName;
    string recordingPath;
    string[] ffmpegArgs;

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        recordingPath = $"/home2/samba/spychalam/recordings/{GetFileName(user)}";
        ffmpegArgs = ["-framerate", "1", "-f", "x11grab", "-i", ":0.0", "-c:v", "libx264", "-g", "10", "-keyint_min", "10", "-preset", "fast", "-f", "mpegts", "-y", "-loglevel", "quiet", recordingPath];
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        recordingPath = $"Z:\\spychalam\\recordings\\{GetFileName(user)}";
        ffmpegArgs = ["-framerate", "1", "-f", "gdigrab", "-i", "desktop", "-c:v", "libx264", "-g", "10", "-keyint_min", "10", "-preset", "fast", "-f", "mpegts", "-y", "-loglevel", "quiet", recordingPath];
    }
    else
    {
        throw new PlatformNotSupportedException("Unknown OS, not supported");
    }

    return Process.Start("ffmpeg", ffmpegArgs) ?? throw new Exception("Failed to start ffmpeg");
}

string GetFileName(string username, string course = "P3")
{
    var url = $"https://ghlabs.mini.pw.edu.pl/create_recordings?username={username}&course={course}";
    using var client = new HttpClient();
    return client.GetStringAsync(url).Result;
}