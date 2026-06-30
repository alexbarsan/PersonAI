using System.Diagnostics;

namespace DreamLens.Api.IntegrationTests;

public sealed class DockerAvailableFactAttribute : FactAttribute
{
    public DockerAvailableFactAttribute()
    {
        if (!DockerAvailability.IsAvailable())
        {
            Skip = "Docker daemon is not available.";
        }
    }

    private static class DockerAvailability
    {
        public static bool IsAvailable()
        {
            try
            {
                if (OperatingSystem.IsWindows() && !File.Exists(@"\\.\pipe\docker_engine"))
                {
                    return false;
                }

                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "docker",
                    ArgumentList = { "version", "--format", "{{.Server.Version}}" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process is null)
                {
                    return false;
                }

                if (!process.WaitForExit(2_000))
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        // Best effort only; a hung docker probe should skip Docker-backed tests.
                    }

                    return false;
                }

                return process.HasExited && process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
