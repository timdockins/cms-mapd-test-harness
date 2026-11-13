namespace CmsMapd.Core;

public sealed record ReplyCode(string Code, char Type, string Title, string ShortName);
public static class ReplyCatalog
{
    // Deliberately bounded to TC 61 scenarios without TRC-dependent field 24 data.
    public static IReadOnlyList<ReplyCode> All { get; } = new ReplyCode[]
    {
        new("004", 'R', "Beneficiary name required", "NEED MEMB NAME"),
        new("006", 'R', "Incorrect birth date", "BAD BIRTH DATE"),
        new("007", 'R', "Invalid beneficiary ID", ""),
        new("008", 'R', "Beneficiary identifier not found", ""),
        new("009", 'R', "No beneficiary match", "NO BENE MATCH"),
        new("011", 'A', "Enrollment accepted as submitted", "ENROLL ACCEPTED"),
        new("038", 'R', "Duplicate enrollment transaction", "DUPLICATE"),
        new("039", 'R', "Already enrolled in same plan", "ALREADY ENROLL"),
        new("102", 'R', "Incorrect or missing application date", "BAD APP DATE"),
        new("107", 'R', "Invalid or missing PBP number", "BAD PBP NUMBER"),
        new("133", 'R', "Invalid secondary insurance flag", "BAD 2 INS FLAG"),
        new("134", 'I', "Missing secondary insurance information", "NO 2 INS INFO")
    };
    public static IReadOnlyList<ReplyCode> Select(string input)
    {
        var codes = input.Split(',', StringSplitOptions.TrimEntries).Select(code =>
        {
            if (code.Length is < 1 or > 3 || code.Any(c => c < '0' || c > '9')) throw new FormatException("Enter numeric TRCs separated by commas.");
            var normalized = code.PadLeft(3, '0');
            return All.SingleOrDefault(x => x.Code == normalized)
                ?? throw new FormatException($"TRC {normalized} is not implemented. Failed transactions (for example 003) belong to BCSS, not DTRR.");
        }).ToArray();
        if (codes.Select(c => c.Code).Distinct().Count() != codes.Length) throw new FormatException("Duplicate TRCs are not allowed.");
        if (codes.Count(c => c.Type is 'A' or 'R') != 1) throw new FormatException("Select exactly one acceptance or rejection TRC.");
        if (codes.Any(c => c.Type == 'R') && codes.Length != 1) throw new FormatException("Informational replies require an accepted enrollment.");
        return codes.OrderBy(c => c.Type == 'I').ToArray();
    }
}
