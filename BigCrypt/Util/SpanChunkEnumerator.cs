using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BigCrypt.Util;

/// <summary>
/// Splits a contiguous sequence of <typeparamref name="T"/>s into chunks of size <paramref name="chunk"/> and wraps each of them in a <see cref="Span{T}"/>
/// </summary>
/// <typeparam name="T">The type of the elements of the sequence</typeparam>
/// <param name="first">The reference to the first element of the sequence</param>
/// <param name="count">The number of elements contained in the sequence</param>
/// <param name="chunk">The maximum size of each chunk</param>
public ref struct SpanChunkEnumerator<T>(ref T first, long count, int chunk)
{
    private readonly ref T _first = ref first;
    
    private long _index;

    public Span<T> Current { get; private set; }

    public SpanChunkEnumerator<T> GetEnumerator() => this;

    public bool MoveNext()
    {
        var div = Static.DivMod(count, chunk, out var mod);
        if (_index > div) return false;
        ref var next = ref Unsafe.Add(ref _first, (UIntPtr)(_index * chunk));
        var length = _index < div ? chunk : (int)mod;
        Current = MemoryMarshal.CreateSpan(ref next, length);
        _index++;
        return true;
    }
}