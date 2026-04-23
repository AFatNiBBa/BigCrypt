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
/// Implementation of <see cref="IOperation"/> to handle the bitwise XOR
/// </summary>
public class XorOperation : IOperation
{
    public static Vector<byte> Combine(Vector<byte> a, Vector<byte> b) => a ^ b;

    public static byte Combine(byte a, byte b) => (byte)(a ^ b);
}

/// <summary>
/// Implementation of <see cref="IOperation"/> to handle the wrapped sum
/// </summary>
public class SumOperation : IOperation
{
    public static Vector<byte> Combine(Vector<byte> a, Vector<byte> b) => a + b;

    public static byte Combine(byte a, byte b) => (byte)(a + b);
}

/// <summary>
/// Implementation of <see cref="IOperation"/> to handle the wrapped subtraction
/// </summary>
public class SubOperation : IOperation
{
    public static Vector<byte> Combine(Vector<byte> a, Vector<byte> b) => a - b;

    public static byte Combine(byte a, byte b) => (byte)(a - b);
}