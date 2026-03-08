using System.Diagnostics;
using BigCrypt.Business;
using BigCrypt.Util;

const int KB = 1024, MIN_THREAD_SIZE = 10 * KB;

if (args is not [ var pathInp, var pathKey, var pathOut]) {
#if DEBUG
    pathInp = "./inp.dat";
    pathKey = "./key.dat";
    pathOut = "./out.dat";
    
#if false
    (pathInp, pathOut) = (pathOut, pathInp);
#endif

#else
    Console.WriteLine($"Usage: {Environment.GetCommandLineArgs()[0]} <pathInp> <pathKey> <pathOut>");
    return 1; 
#endif
}

if (!File.Exists(pathInp))
{
    Console.WriteLine("There's no input file");
    return 2;
}

using var mmapInp = Mmap.Open(pathInp);

if (mmapInp.Size is 0)
{
    Console.WriteLine("The input file is empty");
    return 3;
}

var generateKey = !File.Exists(pathKey);
using var mmapKey = generateKey ? Mmap.Create(pathKey, mmapInp.Size) : Mmap.Open(pathKey);
using var mmapOut = Mmap.Create(pathOut, mmapInp.Size);

var source = new CancellationTokenSource();
var sw = Stopwatch.StartNew();
var progress = 0L;

var display = Task.Run(ShowProgress);

try
{
    var thread = Math.Min(Environment.ProcessorCount, Math.Max(1, mmapInp.Size / MIN_THREAD_SIZE));
    Static.Partition(thread, mmapInp.Size, (offset, size) =>
    {
        var req = new Req(ref mmapInp.First, ref mmapKey.First, ref mmapOut.First, ref progress, generateKey);
        Crypt.Xor(req, offset, size, mmapKey.Size);
    });
}
finally
{
    source.Cancel();
}

await display;

return 0;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

async Task ShowProgress()
{
    var prev = 0;

    try
    {
        while (!source.IsCancellationRequested)
        {
            var text = $"\r{(double)progress * 100 / mmapInp.Size}%";
            Console.Write(text.PadRight(prev, ' '));
            prev = text.Length;
            await Task.Delay(1000, source.Token);
        }
    }
    catch (OperationCanceledException)
    {
        // Skip
    }

    Console.WriteLine();
    Console.WriteLine($"Operation completed in {sw.Elapsed.TotalMilliseconds}ms");
}