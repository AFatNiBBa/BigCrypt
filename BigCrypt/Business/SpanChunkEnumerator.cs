using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BigCrypt.Business;

public ref struct SpanChunkEnumerator<T>(ref T first, long count, int chunk)
{
    private readonly ref T _first = ref first;
    
    private long _index;

    public Span<T> Current { get; private set; }

    public SpanChunkEnumerator<T> GetEnumerator() => this;

    public bool MoveNext()
    {
        var div = Util.DivMod(count, chunk, out var mod);
        if (_index > div) return false;
        ref var next = ref Unsafe.Add(ref _first, (UIntPtr)(_index * chunk));
        var length = _index < div ? chunk : (int)mod;
        Current = MemoryMarshal.CreateSpan(ref next, length);
        _index++;
        return true;
    }
}