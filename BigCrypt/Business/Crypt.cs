using BigCrypt.Util;
using System.Numerics;

namespace BigCrypt.Business;

/// <summary>
/// Class that provides functions to apply the bitwise XOR operation across huge sequences of bytes, using SIMD vectorization when possible
/// </summary>
public static class Crypt
{
    /// <summary>
    /// Applies an operation on a chunk of data.
    /// Handles the "PacMan Effect" if the key is not long enough to last until the end of the chunk
    /// </summary>
    /// <typeparam name="T">The operation to perform</typeparam>
    /// <param name="req">The request containing the references to the memory blocks to use in order to perform the operation</param>
    /// <param name="offset">The offset of the chunk</param>
    /// <param name="size">The size of the chunk</param>
    /// <param name="key">The size of the key sequence</param>
    public static void Op<T>(Req req, long offset, long size, long key) where T : IOperation
    {
        var start = offset % key;
        var available = key - start;

        // Avoids using the "PacMan Effect" if the key is long enough to end this chunk
        if (size <= available)
        {
            Op<T>(req.Move(offset, start), size);
            return;
        }

        // Throws if the key needs to be generated, otherwise it would cause race conditions, size the "PacMan Effect" allows multiple threads to use the same blocks of memory
        if (req.Random) 
            throw new InvalidOperationException("The key needs to be pre-generated when the size of the chunk is bigger than the size of the key");

        // Uses the remaining part of the key
        Op<T>(req.Move(offset, start), available);
        offset += available;
        size -= available;

        // Uses the whole key until a smaller chunk is needed
        var count = Static.DivMod(size, key, out var extra);
        for (long i = 0; i < count; i++, offset += key)
            Op<T>(req.Move(offset, 0), key);

        // Processes the remaining part of the input, if any
        if (extra is 0) return;
        Op<T>(req.Move(offset, 0), extra);
    }

    /// <summary>
    /// Applies an operation on a chunk of data
    /// </summary>
    /// <typeparam name="T">The operation to perform</typeparam>
    /// <param name="req">The request containing the references to the memory blocks to use in order to perform the operation</param>
    /// <param name="size">The size of the chunk</param>
    public static void Op<T>(Req req, long size) where T : IOperation
    {
        // Setup of the references at the beginning of the chunk
        var byteInp = new Ref<byte>(ref req.ByteInp);
        var byteKey = new Ref<byte>(ref req.ByteKey);
        var byteOut = new Ref<byte>(ref req.ByteOut);

        // Key generation
        if (req.Random) Static.Random(ref byteKey.Value, size);

        // Setup of the SIMD references
        ref var vecInp = ref byteInp.As<Vector<byte>>(size, out var countVec, out var countExtra);
        ref var vecKey = ref byteKey.As<Vector<byte>>(size, out _, out _);
        ref var vecOut = ref byteOut.As<Vector<byte>>(size, out _, out _);

        // Vectorized loop
        for (long i = 0; i < countVec; i++, vecInp.Next(), vecKey.Next(), vecOut.Next())
        {
            vecOut.Value = T.Combine(vecInp.Value, vecKey.Value);

            Interlocked.Add(ref req.Progress, Vector<byte>.Count);
        }

        // Scalar loop
        for (var i = 0; i < countExtra; i++, byteInp.Next(), byteKey.Next(), byteOut.Next())
            byteOut.Value = T.Combine(byteInp.Value, byteKey.Value);

        Interlocked.Add(ref req.Progress, countExtra);
    }
}
