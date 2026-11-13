using System.Globalization;

namespace CmsMapd.Core;

// CMS-known values are explicit test data, never inferred from the MBIT.
public sealed record ReplyContext(DateTime ProcessedAt, string SystemTrackingId,
    IReadOnlyDictionary<string, string>? Fields = null);
public sealed record GeneratedReply(IReadOnlyList<string> Records);

public sealed class DtrrGenerator
{
    public const int RecordLength = 800;
    public static IReadOnlyDictionary<string, (int Start, int Width)> ContextFields { get; } =
        new Dictionary<string, (int, int)>(StringComparer.Ordinal)
        {
            ["DisabilityIndicator"] = (53,1), ["HospiceIndicator"] = (54,1),
            ["InstitutionalIndicator"] = (55,1), ["EsrdIndicator"] = (56,1),
            ["EntitlementTypeCode"] = (62,1), ["WorkingAgedIndicator"] = (71,1),
            ["PreviousPartDContractPbp"] = (100,8), ["PriorPbp"] = (121,3),
            ["OutOfAreaFlag"] = (134,1), ["PartCPremium"] = (138,8),
            ["PartDPremium"] = (146,8), ["PremiumWithholdOption"] = (157,1),
            ["CumulativeUncoveredMonths"] = (158,3), ["CreditableCoverageFlag"] = (161,1),
            ["LipsLevel"] = (235,3), ["LowIncomeCopayCategory"] = (238,1),
            ["LowIncomePeriodEffectiveDate"] = (239,8), ["LateEnrollmentPenalty"] = (247,8),
            ["LowIncomePremiumSubsidy"] = (271,8), ["MspStatusFlag"] = (354,1)
        };

    public GeneratedReply Generate(MbitEnrollment input, IReadOnlyList<ReplyCode> codes, ReplyContext context)
    {
        // Revalidate even callers outside the console.
        var selected = ReplyCatalog.Select(string.Join(',', codes.Select(c => c.Code)));
        if (context.SystemTrackingId.Length != 20) throw new FormatException("System tracking ID must be exactly 20 printable ASCII characters.");
        FixedWidth.RequireAscii(context.SystemTrackingId);
        if (context.Fields is not null)
            foreach (var field in context.Fields)
            {
                if (!ContextFields.TryGetValue(field.Key, out var layout)) throw new FormatException($"Unknown context field: {field.Key}.");
                if (field.Value is null) throw new FormatException($"Context field {field.Key} cannot be null.");
                if (field.Value.Length > layout.Width) throw new FormatException($"Context field {field.Key} exceeds {layout.Width} characters.");
                FixedWidth.RequireAscii(field.Value);
            }
        bool accepted = selected.Any(c => c.Type == 'A');
        var records = new List<string>();
        foreach (var code in selected)
        {
            var r = new RecordBuilder(800).Put(1,41,input.Field(1,41))
                .Put(42,1,"T").Put(43,5,input.Contract).Put(57,3,code.Code).Put(60,2,"61")
                .Put(63,8,input.Field(64,8)).Put(72,3,input.Field(43,3))
                .Put(76,8,context.ProcessedAt.ToString("yyyyMMdd",CultureInfo.InvariantCulture))
                .Put(84,1,"0").Put(116,5,input.Contract).Put(124,8,input.Field(52,8))
                .Put(135,3,input.Field(72,3)).Put(154,1,input.Field(46,1))
                .Put(155,1,input.Field(171,1)).Put(156,1,input.Field(99,1))
                .Put(157,1,input.Field(81,1)).Put(162,1,input.Field(98,1))
                .Put(163,15,context.ProcessedAt.ToString("HH.mm.ss.fffff",CultureInfo.InvariantCulture))
                .Put(196,1,input.Field(124,1)).Put(197,1,input.Field(125,1))
                .Put(198,1,input.Field(135,1)).Put(199,20,input.Field(136,20))
                .Put(219,15,input.Field(156,15)).Put(234,1,input.Field(42,1))
                .Put(279,6,input.Field(225,6)).Put(285,10,input.Field(231,10))
                .Put(295,15,input.Field(241,15)).Put(310,20,input.Field(256,20))
                .Put(330,6,input.Field(276,6)).Put(336,10,input.Field(282,10))
                .Put(366,15,code.ShortName).Put(475,20,context.SystemTrackingId)
                .Put(495,15,input.PlanTrackingId)
                .Put(517,10,input.Field(299,10)).Put(527,1,input.Field(309,1))
                .Put(528,8,input.Field(310,8)).Put(536,12,input.Field(318,12))
                .Put(548,10,input.Field(330,10)).Put(558,74,input.Field(340,74));
            if (input.Field(46,1) is "S" or "Y" && code.Code == "011") r.Put(108,2,input.Field(101,2));
            if (code.Type == 'A') r.Put(186,3,input.Field(95,3));
            if (context.Fields is not null)
                foreach (var field in context.Fields)
                {
                    // Cumulative months and coverage reset flag are only populated on acceptance here.
                    if (field.Key is "CumulativeUncoveredMonths" or "CreditableCoverageFlag" && code.Type != 'A') continue;
                    var layout = ContextFields[field.Key];
                    r.Put(layout.Start,layout.Width,field.Value);
                }
            records.Add(r.ToString());
        }
        records.Add(new RecordBuilder(800).Put(1,41,input.Field(1,41)).Put(42,1,"P")
            .Put(43,5,input.Contract).Put(48,600,input.Raw).Put(648,1,accepted ? "A" : "R")
            .Put(649,20,context.SystemTrackingId).Put(669,15,input.PlanTrackingId).ToString());
        return new(records);
    }
}
