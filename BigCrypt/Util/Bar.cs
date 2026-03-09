namespace BigCrypt.Util;

/// <summary>
/// Textual progress bar generator
/// </summary>
/// <param name="total">The total of the progress bar</param>
public class Bar(long total)
{
    public long Value;

    /// <summary>
    /// Creates a string containing a textual representation of the current progress bar
    /// </summary>
    public override string ToString() => ToString(Value, total);

    /// <summary>
    /// Creates a string containing a textual progress bar
    /// </summary>
    /// <param name="value">The value of the progress bar</param>
    /// <param name="total">The total of the progress bar</param>
    public static string ToString(long value, long total)
    {
        const string prefix = "[";
        var suffix = $"] {(double)value * 100 / total:000.000}%";
        var available = Console.WindowWidth - prefix.Length - suffix.Length;
        var full = (int)(value * available / total);
        return $"\r{prefix}{new string('█', full)}{new string('▒', available - full)}{suffix}";
    }
}