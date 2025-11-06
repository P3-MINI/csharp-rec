using System.Diagnostics;
using System.Runtime.InteropServices;

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

Process ffmpeg = Process.Start("ffmpeg", ffmpegArgs);
ffmpeg.WaitForExit();

string GetFileName(string username, string course = "P3")
{
    var url = $"https://ghlabs.mini.pw.edu.pl/create_recordings?username={username}&course={course}";
    using var client = new HttpClient();
    return client.GetStringAsync(url).Result;
}
