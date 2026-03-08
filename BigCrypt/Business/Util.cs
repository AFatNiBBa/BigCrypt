using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace BigCrypt.Business;

/// <summary>
/// Utility functions
/// </summary>
public static class Util
{
    /// <summary>
    /// Creates a <see cref="Business.Ref{T}"/> by adding an offset to a reference
    /// </summary>
    /// <typeparam name="T">The type pointed to by the reference</typeparam>
    /// <param name="value">The reference to the value</param>
    /// <param name="offset">The offset by which to move the reference forward</param>
    public static Ref<T> Ref<T>(ref T value, long offset) => new(ref Unsafe.Add(ref value, (UIntPtr)offset));

    /// <summary>
    /// Executs both a division and a modulus between <paramref name="a"/> and <paramref name="b"/>
    /// </summary>
    /// <typeparam name="T">The numeric type of the operators</typeparam>
    /// <param name="a">The numerator</param>
    /// <param name="b">The denominator</param>
    /// <param name="mod">The resulting modulus between the two numbers</param>
    public static T DivMod<T>(T a, T b, out T mod) where T : IDivisionOperators<T, T, T>, IModulusOperators<T, T, T>
    {
        mod = a % b;
        return a / b;
    }

    /// <summary>
    /// Fills a contiguous block of memory with random bytes.
    /// Splits the block of memory into <see cref="Span{T}"/>s and runs <see cref="RandomNumberGenerator.Fill"/> on each of them
    /// </summary>
    /// <param name="first">The reference to the first element of the sequence</param>
    /// <param name="count">The number of elements contained in the sequence</param>
    public static void Random(ref byte first, long count)
    {
        foreach (var span in new SpanChunkEnumerator<byte>(ref first, count, int.MaxValue))
            RandomNumberGenerator.Fill(span);
    }

    /// <summary>
    /// Splits the processing of a sequence across multiple threads
    /// </summary>
    /// <param name="thread">The number of threads to use</param>
    /// <param name="total">The total length of the sequence to split</param>
    /// <param name="f">The function to apply to each chunk</param>
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