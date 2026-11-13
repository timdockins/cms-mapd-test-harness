# Validation of the initial prototype

Validated October 9, 2026, against the supplied PCUG v19.4.

## Completed

- Compiled all three C# projects with .NET SDK 8.0.412's Roslyn compiler, C# 12, .NET 8 reference assemblies, nullable analysis, and warnings treated as errors.
- Executed the .NET test program: **33 checks passed**.
- Executed the actual console program for 011, 102, and 011 + 134 and included the resulting sample files.
- Executed **11 additional smoke checks**, including an independent byte-position oracle, exact 800-byte ASCII/CRLF framing, verbatim byte preservation, interactive retry after an invalid choice, overwrite prevention, unsupported BCSS code rejection, and individually selected batch scenarios with distinct tracking IDs.
- Inspected the rendered PDF's Layout 3-24 to confirm verbatim offsets and length, and extracted Layouts 3-7, 3-9, 3-23 and Table 3-17 to ground the mappings.

## Environment limitation

The standard `dotnet build` command could not run in this execution container: the SDK CLI/MSBuild failed while querying process metadata from `/proc`, before project evaluation. Compilation and tests were completed by invoking the SDK's Roslyn compiler directly and running the compiled applications under .NET 8. The provided solution/project files have not therefore been validated by a normal MSBuild run here; run the README's build command on your workstation.

No live CMS submission, CMS integration certification, production DTRR comparison, or full MARx business-rule validation was performed. The record layout tests establish this prototype's selected fields and scenario behavior, not complete parity with all CMS-generated data.
