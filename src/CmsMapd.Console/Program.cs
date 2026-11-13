using System.Globalization;
using System.Text;
using System.Text.Json;
using CmsMapd.Core;

try
{
    if (args.Contains("--help"))
    {
        Console.WriteLine("Usage: dotnet run --project src/CmsMapd.Console -- [--input MBIT.txt] [--output DTRR.txt] [--trc 011,134] [--processed-at 2026-10-09T15:00:00] [--profile profile.json]");
        return 0;
    }
    var options = new Dictionary<string,string>();
    var valid = new[] { "--input", "--output", "--trc", "--processed-at", "--profile" };
    for (int i=0; i<args.Length; i+=2)
    {
        if (!valid.Contains(args[i]) || i+1>=args.Length || args[i+1].StartsWith("--") || !options.TryAdd(args[i], args[i+1]))
            throw new FormatException("Unknown, missing or duplicate argument. Use --help.");
    }
    string Ask(string question, string? fallback = null)
    {
        Console.Write(question);
        var answer = Console.ReadLine();
        if (answer is null) throw new FormatException("Input ended before the prompt was answered.");
        return string.IsNullOrWhiteSpace(answer) ? fallback ?? throw new FormatException("A value is required.") : answer.Trim();
    }
    string inputPath = options.GetValueOrDefault("--input") ?? Ask("MBIT file path: ");
    var input = MbitReader.Read(File.ReadAllLines(inputPath));
    string outputPath = options.GetValueOrDefault("--output") ?? Ask("DTRR output path [dtrr.txt]: ", "dtrr.txt");
    if (Path.GetFullPath(inputPath) == Path.GetFullPath(outputPath)) throw new FormatException("Input and output paths must differ.");
    if (File.Exists(outputPath)) throw new IOException("Output exists. Choose a new output path.");
    var processedAt = options.TryGetValue("--processed-at",out var date)
        ? DateTime.ParseExact(date,"yyyy-MM-dd'T'HH:mm:ss",CultureInfo.InvariantCulture)
        : DateTime.UtcNow;
    Dictionary<string,string>? profile = options.TryGetValue("--profile",out var profilePath)
        ? JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(profilePath))
            ?? throw new FormatException("Profile must be a JSON object.") : null;
    Console.WriteLine("TC 61 DTRR test generator | PCUG 19.4 | processing clock: " + processedAt.ToString("O"));
    foreach (var code in ReplyCatalog.All) Console.WriteLine($"  {code.Code} [{code.Type}] {code.Title}");
    Console.WriteLine("CMS-known fields remain blank unless supplied in --profile. No eligibility decisions are performed.");
    var output = new List<string>();
    for (int index=0; index<input.Count; index++)
    {
        IReadOnlyList<ReplyCode> codes;
        if (options.TryGetValue("--trc",out var selection)) codes = ReplyCatalog.Select(selection);
        else
        {
            while (true)
            {
                try { codes = ReplyCatalog.Select(Ask($"Record {index+1}/{input.Count}, {input[index].Contract.Trim()} / PBP {input[index].Field(43,3)}: TRC(s) [011]: ","011")); break; }
                catch (FormatException e) { Console.WriteLine(e.Message); }
            }
        }
        string tracking = "SIM" + (index+1).ToString("D17",CultureInfo.InvariantCulture);
        output.AddRange(new DtrrGenerator().Generate(input[index],codes,new(processedAt,tracking,profile)).Records);
    }
    // CRLF delimited, ASCII, no BOM. FileMode.CreateNew prevents races from overwriting files.
    using (var stream = new FileStream(outputPath,FileMode.CreateNew,FileAccess.Write))
    using (var writer = new StreamWriter(stream,Encoding.ASCII) { NewLine = "\r\n" })
        foreach (var line in output) writer.WriteLine(line);
    Console.WriteLine($"Wrote {output.Count} 800-character records to {outputPath}.");
    return 0;
}
catch (Exception e) when (e is FormatException or IOException or UnauthorizedAccessException or JsonException or ArgumentException)
{
    Console.Error.WriteLine("Error: " + e.Message);
    return 1;
}
