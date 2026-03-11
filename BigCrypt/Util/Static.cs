using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace BigCrypt.Util;

/// <summary>
/// Utility functions
/// </summary>
public static class Static
{
    public const int KB = 1024;

    /// <summary>
    /// Offsets a reference by the given amount of <typeparamref name="T"/> values and checks for overflows
    /// </summary>
    /// <typeparam name="T">The type of the value pointed at by the sequence</typeparam>
    /// <param name="first">The reference to offset</param>
    /// <param name="offset">The number of <typeparamref name="T"/> by which to offset the reference</param>
    public static ref T Offset<T>(ref T first, long offset) => ref Unsafe.Add(ref first, checked((UIntPtr)offset));

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
    /// Writes the contents <paramref name="source"/> into <paramref name="dest"/> and advances <paramref name="dest"/> by the length of <paramref name="source"/>
    /// </summary>
    /// <typeparam name="T">The type of the elements of the two sequences</typeparam>
    /// <param name="dest">The destination sequence</param>
    /// <param name="source">The source sequence</param>
    public static void Write<T>(ref Span<T> dest, ReadOnlySpan<T> source)
    {
        source.CopyTo(dest);
        dest = dest[source.Length..];
    }

    /// <summary>
    /// Writes <paramref name="value"/> <paramref name="count"/> times into <paramref name="dest"/> and advances <paramref name="dest"/> by <paramref name="count"/>
    /// </summary>
    /// <typeparam name="T">The type of the elements of the two sequences</typeparam>
    /// <param name="dest">The destination sequence</param>
    /// <param name="value">The value to write into the sequence</param>
    /// <param name="count">The number of times to write the value</param>
    public static void Write<T>(ref Span<T> dest, T value, int count)
    {
        dest[..count].Fill(value);
        dest = dest[count..];
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