using System.Runtime.CompilerServices;

namespace BigCrypt.Business;

public ref struct Ref<T>(ref T value)
{
    public ref T Value = ref value;

    public void Next() => Value = ref Unsafe.Add(ref Value, 1);

    public ref Ref<R> As<R>(long countInp, out long countOut, out long extraBytes)
    {
        var size = countInp * Unsafe.SizeOf<T>();
        countOut = Util.DivMod(size, Unsafe.SizeOf<R>(), out extraBytes);
        return ref Unsafe.As<Ref<T>, Ref<R>>(ref Unsafe.AsRef(in this));
    }
}