namespace BigCrypt.Util;

/// <summary>
/// Textual progress bar generator
/// </summary>
/// <param name="total">The total of the progress bar</param>
public class Bar(long total)
{
    public long Value;

    /// <summary>
    /// Writes the current progress bar to the <see cref="Console"/>
    /// </summary>
    public void WriteToConsole() => WriteToConsole(Value, total);

    /// <summary>
    /// Writes a textual progress bar to the <see cref="Console"/>
    /// </summary>
    /// <param name="value">The value of the progress bar</param>
    /// <param name="total">The total of the progress bar</param>
    public static void WriteToConsole(long value, long total)
    {
        const string RESTORE = "\r";
        Span<char> res = stackalloc char[RESTORE.Length + Console.WindowWidth];
        var dest = res;
        Static.Write(ref dest, RESTORE);
        if (!TryWrite(dest, value, total)) return;
        Console.Write(res);
    }

    /// <summary>
    /// Writes a textual progress bar to a <see cref="Span{T}"/>
    /// </summary>
    /// <param name="dest">The destination in which to try to write the progress bar</param>
    /// <param name="value">The value of the progress bar</param>
    /// <param name="total">The total of the progress bar</param>
    public static bool TryWrite(Span<char> dest, long value, long total)
    {
        const string PREFIX = "[", SUFFIX = "] ", FORMAT = "000.0000", PERC = "%";
        const char FULL = '█', EMPTY = '▒';

        var min = PREFIX.Length + SUFFIX.Length + FORMAT.Length + PERC.Length;
        if (dest.Length < min) return false;
        var available = dest.Length - min; // I take the length before I "shrink" the destination
        Static.Write(ref dest, PREFIX);
        var full = (int)(value * available / total);
        Static.Write(ref dest, FULL, full);
        Static.Write(ref dest, EMPTY, available - full);
        Static.Write(ref dest, SUFFIX);
        var perc = (double)value * 100 / total;
        if (!perc.TryFormat(dest, out var written, FORMAT)) return false;
        if (written != FORMAT.Length) return false;
        PERC.CopyTo(dest[written..]);
        return true;
    }

}