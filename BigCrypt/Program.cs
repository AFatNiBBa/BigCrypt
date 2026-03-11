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
    Console.WriteLine("The input file does not exist");
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

var bar = new Bar(mmapInp.Size);

await using (Progress.Report(bar))
{
    var thread = Math.Min(Environment.ProcessorCount, Math.Max(1, mmapInp.Size / MIN_THREAD_SIZE));
    Static.Partition(thread, mmapInp.Size, (offset, size) =>
    {
        var req = new Req(ref mmapInp.First, ref mmapKey.First, ref mmapOut.First, ref bar.Value, generateKey);
        Crypt.Xor(req, offset, size, mmapKey.Size);
    });
}

return 0;