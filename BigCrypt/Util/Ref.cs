using System.Runtime.CompilerServices;

namespace BigCrypt.Util;

/// <summary>
/// Utility wrapper over a reference
/// </summary>
/// <typeparam name="T">The type of the wrapped reference</typeparam>
/// <param name="value">The reference to wrap</param>
public ref struct Ref<T>(ref T value)
{
    public ref T Value = ref value;

    /// <summary>
    /// Points the reference to the next element, as if it were an array of <typeparamref name="T"/>
    /// </summary>
    public void Next() => Value = ref Unsafe.Add(ref Value, 1);

    /// <summary>
    /// Reinterprets the current <see cref="Ref{T}"/> as if it pointed to an <typeparamref name="R"/> instead of a <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="R">The new type of the reference</typeparam>
    /// <param name="countInp">The number of consecutive <typeparamref name="T"/>s</param>
    /// <param name="countOut">The resulting number of consecutive <typeparamref name="R"/>s</param>
    /// <param name="extraBytes">The resulting number of extra bytes that couldn't be fitted inside an integer amount of <typeparamref name="R"/>s</param>
    public ref Ref<R> As<R>(long countInp, out long countOut, out long extraBytes)
    {
        var size = countInp * Unsafe.SizeOf<T>();
        countOut = Static.DivMod(size, Unsafe.SizeOf<R>(), out extraBytes);
        return ref Unsafe.As<Ref<T>, Ref<R>>(ref Unsafe.AsRef(in this));
    }
}