namespace CmsMapd.Core;

// Positions use the PCUG's 1-based notation throughout.
public static class FixedWidth
{
    public static string Read(string record, int start, int length) => record.Substring(start - 1, length);
    public static void RequireAscii(string value)
    {
        if (value.Any(c => c < ' ' || c > '~'))
            throw new FormatException("Records must contain printable ASCII only.");
    }
}

public sealed class RecordBuilder(int length)
{
    private readonly char[] buffer = new string(' ', length).ToCharArray();
    public RecordBuilder Put(int start, int width, string value)
    {
        FixedWidth.RequireAscii(value);
        if (value.Length > width) throw new FormatException($"Field at {start} exceeds width {width}.");
        if (start < 1 || start + width - 1 > buffer.Length) throw new ArgumentOutOfRangeException(nameof(start));
        Array.Fill(buffer, ' ', start - 1, width);
        value.CopyTo(0, buffer, start - 1, value.Length);
        return this;
    }
    public override string ToString() => new(buffer);
}
