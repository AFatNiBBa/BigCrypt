using BigCrypt.Business;
using BigCrypt.Util;
using System.IO.MemoryMappedFiles;

namespace BigCrypt;

/// <summary>
/// Implementation of the command line interface of the application
/// </summary>
internal static class Cli
{
    private const int MIN_THREAD_SIZE = 10 * Static.KB;

    /// <summary>
    /// Performs an operation between the input and key file and writes the result to the output one.
    /// If the random flag is set, the contents of the key file will be replaced by a random sequence of bytes as long as the input file
    /// </summary>
    /// <typeparam name="T">The operation to perform</typeparam>
    /// <param name="mmapInp">The path to the input file</param>
    /// <param name="mmapKey">The path to the key file</param>
    /// <param name="mmapOut">The path to the output file</param>
    /// <param name="random">Whether to generate the key</param>
    public static Task Op<T>(Mmap mmapInp, Mmap mmapKey, Mmap mmapOut, bool random) where T : IOperation =>
        Run(mmapInp.Size, (bar, offset, size) =>
        {
            var req = new Req(ref mmapInp.First, ref mmapKey.First, ref mmapOut.First, ref bar.Value, random);
            Crypt.Op<T>(req, offset, size, mmapKey.Size);
        });

    /// <summary>
    /// Performs an operation between the input and key file and writes the result to the output one.
    /// If the output file is not specified, the input file will be modified in place.
    /// If the random flag is set, the contents of the key file will be replaced by a random sequence of bytes as long as the input file
    /// </summary>
    /// <typeparam name="T">The operation to perform</typeparam>
    /// <param name="pathInp">The path to the input file</param>
    /// <param name="pathKey">The path to the key file</param>
    /// <param name="pathOut">The path to the output file</param>
    /// <param name="random">Whether to generate the key</param>
    public static async Task Op<T>(string pathInp, string pathKey, string? pathOut, bool random) where T : IOperation
    {
        using var mmapInp = Mmap.Open(pathInp, pathOut is null ? MemoryMappedFileAccess.ReadWrite : MemoryMappedFileAccess.Read);
        using var mmapKey = random ? Mmap.Create(pathKey, mmapInp.Size) : Mmap.Open(pathKey);

        if (pathOut is null)
        {
            await Op<T>(mmapInp, mmapKey, mmapInp, random);
            return;
        }

        using var mmapOut = Mmap.Create(pathOut, mmapInp.Size);
        await Op<T>(mmapInp, mmapKey, mmapOut, random);
    }

    /// <summary>
    /// Fills a file with a random sequence of bytes of the specified size
    /// </summary>
    /// <param name="path">The path to the file to fill with random data</param>
    /// <param name="total">The desired size</param>
    public static async Task Rnd(string path, long total)
    {
        using var mmap = Mmap.Create(path, total);

        await Run(total, (bar, offset, size) =>
        {
            ref var first = ref Static.Offset(ref mmap.First, offset);
            Static.Random(ref first, size);
            Interlocked.Add(ref bar.Value, size);
        });
    }

    /// <summary>
    /// Splits the processing of a sequence across multiple threads and uses a textual progress bar to display the progress of the operation
    /// </summary>
    /// <param name="total">The total length of the sequence to split</param>
    /// <param name="f">The function to apply to each chunk</param>
    private static async Task Run(long total, Action<Bar, long, long> f)
    {
        var bar = new Bar(total);
        
        await using (Progress.Report(bar))
        {
            var thread = Math.Min(Environment.ProcessorCount, Math.Max(1, total / MIN_THREAD_SIZE));
            Static.Partition(thread, total, (offset, size) => f(bar, offset, size));
        }
    }
}