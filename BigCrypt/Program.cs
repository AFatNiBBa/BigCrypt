using BigCrypt.Business;
using System.Diagnostics;
using System.Numerics;

const int KB = 1024, MIN_THREAD_SIZE = 10 * KB;

#if false

using var mmap = Mmap.Create(@"C:\Users\ZioPe\Downloads\big.txt", 8L * KB * KB * KB);

Partition(mmap.Size, (offset, size) =>
{
    ref var first = ref Util.Index(ref mmap.First, offset);
    Util.Random(ref first, size);
});

return 0;

#endif

// TODO: Dopo essere arrivato al 100%, comunque ci impiega un bel po' a finire (Potrebbe essere addirittura più veloce senza thread)

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
    Console.WriteLine("Deve già esistere almeno il file di input");
    return 2;
}

var randomKey = !File.Exists(pathKey);

using var mmapInp = Mmap.Open(pathInp);
using var mmapKey = randomKey ? Mmap.Create(pathKey, mmapInp.Size) : Mmap.Open(pathKey);
using var mmapOut = Mmap.Create(pathOut, mmapInp.Size);

var source = new CancellationTokenSource();
var sw = Stopwatch.StartNew();
var progress = 0L;

var display = Task.Run(async () =>
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
        // Ignora
    }

    Console.WriteLine();
    Console.WriteLine($"Operazione completata in {sw.Elapsed.TotalMilliseconds}");
});

try
{
    Partition(mmapInp.Size, (offset, size) =>
    {
        // Gestione effetto PacMan in caso di chiave più piccola dell'input
        var chunk = mmapKey.Size;
        var start = offset % chunk;
        var available = chunk - start;

        // Effetto PacMan non necessario, basta un solo chunk
        if (size <= available)
        {
            ProcessChunk(offset, start, size);
            return;
        }

        // Parte iniziale
        ProcessChunk(offset, start, available);
        offset += available;
        size -= available;

        // Giri completi di chiave
        var count = Util.DivMod(size, chunk, out var extra);
        for (long i = 0; i < count; i++, offset += chunk)
            ProcessChunk(offset, 0, chunk);

        // Parte finale
        if (extra is 0) return;
        ProcessChunk(offset, 0, extra);
    });
}
finally
{
    source.Cancel();
}

await display;

return 0;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void ProcessChunk(long offsetInp, long offsetKey, long size)
{
    // Preparazione dei riferimenti all'inizio del chunk
    var byteInp = Util.Ref(ref mmapInp.First, offsetInp);
    var byteKey = Util.Ref(ref mmapKey.First, offsetKey);
    var byteOut = Util.Ref(ref mmapOut.First, offsetInp);

    // Generazione chiave
    if (randomKey) Util.Random(ref byteKey.Value, size); // L'effetto PacMan avviene solo sulle chiavi pre-esistenti, quindi la generazione di quella randomica non causa comunque race-conditions

    // Preparazione dei riferimenti SIMD
    ref var vecInp = ref byteInp.As<Vector<byte>>(size, out var countVec, out var countExtra);
    ref var vecKey = ref byteKey.As<Vector<byte>>(size, out _, out _);
    ref var vecOut = ref byteOut.As<Vector<byte>>(size, out _, out _);

    // Ciclo della parte vettorizzabile
    for (long i = 0; i < countVec; i++, vecInp.Next(), vecKey.Next(), vecOut.Next())
    {
        vecOut.Value = vecInp.Value ^ vecKey.Value;

        Interlocked.Add(ref progress, Vector<byte>.Count);
    }

    // Ciclo della parte non vettorizzabile
    for (var i = 0; i < countExtra; i++, byteInp.Next(), byteKey.Next(), byteOut.Next())
        byteOut.Value = (byte)(byteInp.Value ^ byteKey.Value);

    Interlocked.Add(ref progress, countExtra);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

static void Partition(long size, Action<long, long> f)
{
    var thread = Math.Min(Environment.ProcessorCount, Math.Max(1, size / MIN_THREAD_SIZE));
    Util.Partition(thread, size, f);
}