namespace CmsMapd.Core;

public sealed record MbitEnrollment
{
    public const int RecordLength = 600;
    public string Raw { get; }
    public string Field(int start, int width) => FixedWidth.Read(Raw, start, width);
    public string Contract => Field(47, 5);
    public string PlanTrackingId => Field(210, 15);
    public MbitEnrollment(string raw)
    {
        if (raw.Length != RecordLength) throw new FormatException($"MBIT detail must be exactly 600 characters; found {raw.Length}. Preserve trailing spaces.");
        FixedWidth.RequireAscii(raw);
        if (FixedWidth.Read(raw, 60, 2) != "61") throw new FormatException("Only TC 61 is supported.");
        Raw = raw; // Do not validate business rules: malformed field values are useful rejection fixtures.
    }
}

public static class MbitReader
{
    public static IReadOnlyList<MbitEnrollment> Read(IEnumerable<string> lines)
    {
        var result = new List<MbitEnrollment>();
        int lineNumber = 0;
        foreach (var line in lines)
        {
            lineNumber++;
            try
            {
                if (line.StartsWith("AAAAAAHEADER", StringComparison.Ordinal))
                {
                    if (lineNumber != 1 || line.Length != 600) throw new FormatException("A 600-character header is allowed only on the first line.");
                    FixedWidth.RequireAscii(line);
                    continue;
                }
                result.Add(new MbitEnrollment(line));
            }
            catch (FormatException e) { throw new FormatException($"Line {lineNumber}: {e.Message}", e); }
        }
        if (result.Count == 0) throw new FormatException("Input contains no enrollment details.");
        return result;
    }
}
