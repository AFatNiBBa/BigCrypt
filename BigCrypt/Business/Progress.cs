using BigCrypt.Util;
using System.Diagnostics;

namespace BigCrypt.Business;

/// <summary>
/// Functions to start periodic reporting of the progress of an operation
/// </summary>
public class Progress
{
    /// <summary>
    /// Starts periodically reporting the progress of <paramref name="bar"/>
    /// </summary>
    /// <param name="bar">The textual progress bar through which to report the progress</param>
    /// <param name="ms">Interval between each report</param>
    /// <returns>An object through which to stop the reporting</returns>
    public static IAsyncDisposable Report(Bar bar, int ms = 1000)
    {
        var source = new CancellationTokenSource();
        return new Stopper(source, Loop(bar, ms, source.Token));
    }

    /// <summary>
    /// Periodically reports the progress of <paramref name="bar"/>
    /// </summary>
    /// <param name="bar">The textual progress bar through which to report the progress</param>
    /// <param name="ms">Interval between each report</param>
    /// <param name="token">A token that tells when to stop the reporting</param>
    private static async Task Loop(Bar bar, int ms, CancellationToken token)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            while (!token.IsCancellationRequested)
            {
                bar.WriteToConsole();
                await Task.Delay(ms, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Skip
        }

        Bar.WriteToConsole(1, 1);
        Console.WriteLine();
        Console.WriteLine($"Operation completed in {sw.Elapsed.TotalMilliseconds}ms");
    }
}

/// <summary>
/// On disposal, stops an operation and waits for it to end
/// </summary>
/// <param name="source">The object through which to stop the operation</param>
/// <param name="task">The object through which to wait for the end of the operation</param>
file class Stopper(CancellationTokenSource source, Task task) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        using (source)
        {
            await source.CancelAsync();
            await task;
        }
    }
}