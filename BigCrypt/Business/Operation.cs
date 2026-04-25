using System.Numerics;

namespace BigCrypt.Business;

/// <summary>
/// Abstract operation that can be executed on a sequence of <see cref="byte"/>s.
/// The operation can be executed both in a scalar and vectorized way
/// </summary>
public interface IOperation
{
    static abstract Vector<byte> Combine(Vector<byte> a, Vector<byte> b);

    static abstract byte Combine(byte a, byte b);
}

/// <summary>
/// Implementation of <see cref="IOperation"/> that performs the bitwise XOR.
/// It's its own inverse, so you can't apply the same key multiple times.
/// Appling the key to the input is the same as applying the input to the key
/// </summary>
public class XorOperation : IOperation
{
    public static Vector<byte> Combine(Vector<byte> a, Vector<byte> b) => a ^ b;

    public static byte Combine(byte a, byte b) => (byte)(a ^ b);
}

/// <summary>
/// Implementation of <see cref="IOperation"/> that performs the wrapped sum.
/// Use <see cref="SubOperation"/> to reverse the operation.
/// The same key can be applied multiple times.
/// If you apply multiple keys, the order in which they're applied doesn't matter, same goes for the order in which you apply them to reverse the operation.
/// Appling the key to the input is the same as applying the input to the key
/// </summary>
public class SumOperation : IOperation
{
    public static Vector<byte> Combine(Vector<byte> a, Vector<byte> b) => a + b;

    public static byte Combine(byte a, byte b) => (byte)(a + b);
}

/// <summary>
/// Implementation of <see cref="IOperation"/> that performs the wrapped subtraction.
/// Use <see cref="SumOperation"/> to reverse the operation.
/// The same key can be applied multiple times.
/// If you apply multiple keys, the order in which they're applied doesn't matter, same goes for the order in which you apply them to reverse the operation
/// </summary>
public class SubOperation : IOperation
{
    public static Vector<byte> Combine(Vector<byte> a, Vector<byte> b) => a - b;

    public static byte Combine(byte a, byte b) => (byte)(a - b);
}