using System.Runtime.CompilerServices;

namespace BigCrypt.Business;

/// <summary>
/// Request for the operations provided by the <see cref="Crypt"/> <see langword="class"/>
/// </summary>
/// <param name="byteInp">The reference to the first byte of the input data</param>
/// <param name="byteKey">The reference to the first byte of the key data</param>
/// <param name="byteOut">The reference to the first byte of the output data</param>
/// <param name="progress">The reference to the counter of already processed data</param>
/// <param name="random">Whether to generate the key instead of reading it from file</param>
public readonly ref struct Req(ref byte byteInp, ref byte byteKey, ref byte byteOut, ref long progress, bool random)
{
    public readonly ref byte ByteInp = ref byteInp;

    public readonly ref byte ByteKey = ref byteKey;

    public readonly ref byte ByteOut = ref byteOut;

    public readonly ref long Progress = ref progress;

    public readonly bool Random = random;

    /// <summary>
    /// Creates a new request with the references moved by the specified offsets
    /// </summary>
    /// <param name="offsetInp">The offset to apply to both the input and the output references</param>
    /// <param name="offsetKey">The offset to apply to the key reference</param>
    public Req Move(long offsetInp, long offsetKey)
    {
        ref var byteInp = ref Unsafe.Add(ref ByteInp, (UIntPtr)offsetInp);
        ref var byteKey = ref Unsafe.Add(ref ByteKey, (UIntPtr)offsetKey);
        ref var byteOut = ref Unsafe.Add(ref ByteOut, (UIntPtr)offsetInp);
        return new(ref byteInp, ref byteKey, ref byteOut, ref Progress, Random);
    }
}