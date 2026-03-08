using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace BigCrypt.Business;

public static class Util
{
    public static Ref<T> Ref<T>(ref T value, long offset) => new(ref Unsafe.Add(ref value, (UIntPtr)offset));

    public static T DivMod<T>(T a, T b, out T mod) where T : IDivisionOperators<T, T, T>, IModulusOperators<T, T, T>
    {
        mod = a % b;
        return a / b;
    }

    public static void Random(ref byte first, long count)
    {
        foreach (var span in new SpanChunkEnumerator<byte>(ref first, count, int.MaxValue))
            RandomNumberGenerator.Fill(span);
    }

    public static void Partition(long thread, long total, Action<long, long> f)
    {
        var chunk = DivMod(total, thread, out var extra);
        Parallel.For(0, thread, i =>
        {
            var offset = i * chunk;
            var size = chunk + (i == thread - 1 ? extra : 0);
            f(offset, size);
        });
    }
}