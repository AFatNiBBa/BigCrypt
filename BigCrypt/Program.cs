using BigCrypt.Business;
using System.Diagnostics;
using System.Numerics;

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

var randomKey = !File.Exists(pathKey);
using var mmapKey = randomKey ? Mmap.Create(pathKey, mmapInp.Size) : Mmap.Open(pathKey);
using var mmapOut = Mmap.Create(pathOut, mmapInp.Size);

var source = new CancellationTokenSource();
var sw = Stopwatch.StartNew();
var progress = 0L;

var display = Task.Run(ShowProgress);

try
{
    var thread = Math.Min(Environment.ProcessorCount, Math.Max(1, mmapInp.Size / MIN_THREAD_SIZE));
    Util.Partition(thread, mmapInp.Size, ProcessThread);
}
finally
{
    source.Cancel();
}

await display;

return 0;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void ProcessThread(long offset, long size)
{
    var chunk = mmapKey.Size;
    var start = offset % chunk;
    var available = chunk - start;

    // Avoids using the "PacMan Effect" if the key is long enough to end this chunk
    if (size <= available)
    {
        ProcessChunk(offset, start, size);
        return;
    }

    // Uses the remaining part of the key
    ProcessChunk(offset, start, available);
    offset += available;
    size -= available;

    // Uses the whole key until a smaller chunk is needed
    var count = Util.DivMod(size, chunk, out var extra);
    for (long i = 0; i < count; i++, offset += chunk)
        ProcessChunk(offset, 0, chunk);

    // Processes the remaining part of the input, if any
    if (extra is 0) return;
    ProcessChunk(offset, 0, extra);
}

void ProcessChunk(long offsetInp, long offsetKey, long size)
{
    // Setup of the references at the beginning of the chunk
    var byteInp = Util.Ref(ref mmapInp.First, offsetInp);
    var byteKey = Util.Ref(ref mmapKey.First, offsetKey);
    var byteOut = Util.Ref(ref mmapOut.First, offsetInp);

    // Key generation
    // The "PacMan Effect" only happens on pre-existing keys, so the generation of the random one doesn't cause race conditions
    if (randomKey) Util.Random(ref byteKey.Value, size);

    // Setup of the SIMD references
    ref var vecInp = ref byteInp.As<Vector<byte>>(size, out var countVec, out var countExtra);
    ref var vecKey = ref byteKey.As<Vector<byte>>(size, out _, out _);
    ref var vecOut = ref byteOut.As<Vector<byte>>(size, out _, out _);

    // Vectorized loop
    for (long i = 0; i < countVec; i++, vecInp.Next(), vecKey.Next(), vecOut.Next())
    {
        vecOut.Value = vecInp.Value ^ vecKey.Value;

        Interlocked.Add(ref progress, Vector<byte>.Count);
    }

    // Scalar loop
    for (var i = 0; i < countExtra; i++, byteInp.Next(), byteKey.Next(), byteOut.Next())
        byteOut.Value = (byte)(byteInp.Value ^ byteKey.Value);

    Interlocked.Add(ref progress, countExtra);
}

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