using System.ComponentModel;
using System.Diagnostics;

namespace AnvilTests;

internal static class JavaToolRunner
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public static async Task<(int ExitCode, string StandardOutput, string StandardError)> RunAsync(
        string executable,
        params string[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    $"Failed to start required Java tool '{executable}'.");
            }
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException(
                $"Required Java tool '{executable}' was not found on PATH.",
                exception);
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        using var cancellationTokenSource = new CancellationTokenSource(Timeout);

        try
        {
            await process.WaitForExitAsync(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException exception)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            throw new TimeoutException(
                $"Java tool '{executable}' did not finish within {Timeout.TotalSeconds} seconds.",
                exception);
        }

        return (
            process.ExitCode,
            await standardOutputTask,
            await standardErrorTask);
    }
}
