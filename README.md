# CMS MAPD transaction test harness

A .NET 8 console prototype that reads MARx Batch Input Transaction (MBIT) enrollment details (TC 61), asks which Transaction Reply Code (TRC) to return, and writes Daily Transaction Reply Report (DTRR) records.

Specification: the supplied **PCUG v19.4, October 1, 2026**. Layout 3-7 (MBIT header), Layout 3-9 (TC 61), Layout 3-23 (DTRR), Layout 3-24 (verbatim), and Table 3-17 (TRC definitions). See `docs/Specification.md` for positions, conditional rules, and deliberate limits.

## Quick start

Install the .NET 8 SDK, extract this archive, and open a terminal in `CmsMapd.TestHarness` (the directory containing this README). The solution also opens in Visual Studio or Rider.

```powershell
dotnet build CmsMapd.TestHarness.sln
dotnet run --project src/CmsMapd.Console -- --input samples/enrollment-tc61.mbit.txt --output first-test.dtrr.txt
```

For each enrollment, enter `011` for acceptance, a supported rejection such as `102`, or `011,134` for acceptance plus an informational reply. Enter accepts the default `011`. The program prints a menu. It takes a file with one or more **exactly 600-character** TC 61 detail lines, optionally preceded by one 600-character MBIT header. Unsupported transaction types cause the entire run to fail before output is created. Preserve trailing spaces.

The generated file contains one `T` record per selected TRC and one `P` verbatim record per input enrollment. Every record has 800 printable ASCII characters, followed by CRLF; no BOM, DTRR header, or trailer is added. The original 600-character detail appears intact inside the `P` record. The program refuses to overwrite an existing output file.

## Repeatable test scenarios

```powershell
# Accepted enrollment with explicit, synthetic CMS-known test values.
dotnet run --project src/CmsMapd.Console -- --input samples/enrollment-tc61.mbit.txt --output accepted.dtrr.txt --trc 011 --processed-at 2026-10-09T15:30:45 --profile samples/accepted-profile.json

# Rejection (no business decision is performed; you request the scenario).
dotnet run --project src/CmsMapd.Console -- --input samples/enrollment-tc61.mbit.txt --output rejected.dtrr.txt --trc 102 --processed-at 2026-10-09T15:30:45

# Acceptance with an additional informational reply.
dotnet run --project src/CmsMapd.Console -- --input samples/enrollment-tc61.mbit.txt --output informational.dtrr.txt --trc 011,134 --processed-at 2026-10-09T15:30:45 --profile samples/accepted-profile.json
```

`--trc` applies the selected scenario to every input enrollment; omit it to choose separately for each. `--processed-at` is a test clock with no timezone conversion. Without it, the clock uses UTC. Synthetic system tracking IDs are `SIM` plus a 17-digit input sequence; every response to a given input shares its ID. IDs repeat across separate runs, enabling deterministic comparisons; they are not a production uniqueness scheme.

Supported TRCs: **004, 006, 007, 008, 009, 011, 038, 039, 102, 107, 133, 134**. Exactly one acceptance or rejection is required. Informational replies require acceptance. Unsupported TRCs are rejected rather than treated as arbitrary codes. Failure codes such as 003 belong to BCSS, not DTRR.

## CMS-known values

The MBIT does not contain the CMS beneficiary history, entitlement, final premiums, or cumulative uncovered-month balance. By default, these fields remain blank. For a fuller acceptance fixture, use the included synthetic profile, or provide a JSON object of fields listed in `docs/Specification.md`. Values are **already formatted field text**: for example `"CumulativeUncoveredMonths": "005"` and `"PartDPremium": "   25.00"`. Width and ASCII checks are enforced; CMS business meanings and cross-field combinations are the responsibility of the test scenario author.

These defaults are intentionally partial fixtures. Use an explicit profile if your receiving system expects entitlement, premium, or other CMS-known values to be populated. This is not a complete MARx emulator. It does not check CMS beneficiary data, execute election-period policy, automatically discover additional TRCs, transmit to CMS, generate BCSS, or emulate CMS record ordering.

The sample enrollment is invented test data, not a production-ready enrollment. Its rejection samples deliberately use the same source enrollment to exercise your consumer's handling of a selected response. To test data/reason consistency, change the MBIT fields to match the rejection you are simulating.

## Projects and tests

- `CmsMapd.Core`: fixed-width reader, bounded TRC catalog, scenario context, and DTRR generator.
- `CmsMapd.Console`: input/output, interactive prompts, and repeatable command options.
- `CmsMapd.Tests`: dependency-free executable tests for field positions, lengths, conditional fields, verbatim copying, correlation, and invalid inputs. No external NuGet packages are needed.

```powershell
dotnet run --project tests/CmsMapd.Tests -- .
```

Run from the solution directory so `.` contains `samples`. This is an executable test suite; `dotnet test` is not its runner. Validation details are in `docs/Validation.md`.

## Extending the prototype

Add a TRC only after reviewing its TC applicability, field 18 semantics, field 24 union data, and conditional fields in Layout 3-23. Codes requiring CMS corrections, prior enrollment, LIS periods, or dates need explicit scenario models before they can be supported safely. Do not simply add every PCUG TRC to the menu.

A useful next increment is TRC 121 with explicit LIS periods and multiple replies per enrollment, followed by the remaining TC 61 acceptance variants and BCSS failures. Additional transaction types should have their own MBIT parsers and mapping policies; the fixed-width primitives and DTRR writer can be reused.
