# PCUG 19.4 implementation notes

All positions are 1-based, inclusive. References are to the supplied PCUG, not an independently retrieved version. MBIT tables use printed pages 30 and 53–62; DTRR tables use section pages 3-8–3-22; Table 3-17 starts at 3-84.

## Record contracts

| Record | Length | Specification |
|---|---:|---|
| MBIT header | 600 | Layout 3-7, printed page 30 |
| TC 61 enrollment detail | 600 | Layout 3-9, printed pages 53–62 |
| DTRR TRC (`T`) | 800 | Layout 3-23, pages 3-9–3-21 |
| DTRR verbatim (`P`) | 800 | Layout 3-24, page 3-22 |

The reader recognizes `AAAAAAHEADER` only on the first line and checks its width/ASCII. It does not validate CCM, IDM user ID, or batch approval fields. The 2026 MBIT header uses IDM User ID at 47–120. No assumptions based on older header formats are needed because headers are not transformed into DTRR records.

## TC 61 to DTRR T mapping

| Field | MBIT positions | DTRR positions |
|---|---|---|
| Beneficiary ID, surname, first name, middle initial, sex, DOB | 1–41 | 1–41 |
| Record type | Generated `T` | 42 |
| Contract | 47–51 | 43–47 |
| TRC | Selected scenario | 57–59 |
| TC | Validated `61` | 60–61 |
| Effective date | 64–71 | 63–70 |
| PBP | 43–45 | 72–74 |
| Transaction date | Processing clock, yyyyMMdd | 76–83 |
| UI flag | Generated `0` (batch) | 84 |
| SEP reason | 101–102 | 108–109 |
| Source ID | Submitted contract (plan batch assumption) | 116–120 |
| Application date | 52–59 | 124–131 |
| Segment | 72–74 | 135–137 |
| Election type | 46 | 154 |
| Enrollment source | 171 | 155 |
| Part D opt-out | 99 | 156 |
| Premium withhold option | 81; optional profile override | 157 |
| Employer subsidy override | 98 | 162 |
| Processing timestamp | Clock, HH.mm.ss.fffff, right padded to 15 | 163–177 |
| Submitted uncovered months | 95–97 | 186–188 |
| Preferred language | 124 | 196 |
| Accessible format | 125 | 197 |
| Secondary insurance flag | 135 | 198 |
| Secondary Rx ID | 136–155 | 199–218 |
| Secondary Rx group | 156–170 | 219–233 |
| EGHP | 42 | 234 |
| Part D Rx BIN | 225–230 | 279–284 |
| Part D Rx PCN | 231–240 | 285–294 |
| Part D Rx group | 241–255 | 295–309 |
| Part D Rx ID | 256–275 | 310–329 |
| Secondary Rx BIN | 276–281 | 330–335 |
| Secondary Rx PCN | 282–291 | 336–345 |
| TRC short name | Catalog test label; see caveat below | 366–380 |
| System tracking ID | Synthetic per input | 475–494 |
| Plan tracking ID | 210–224 | 495–509 |
| NPN | 299–308 | 517–526 |
| OEC indicator | 309 | 527 |
| OEC application date | 310–317 | 528–535 |
| OEC application number | 318–329 | 536–547 |
| Phone | 330–339 | 548–557 |
| Email | 340–413 | 558–631 |

Every field not populated by these mappings or the explicit context profile is space-filled. Core field positions are protected from profile overrides.

## Conditional rules and assumptions

- In this catalog, effective date is the submitted effective date. TRCs requiring a different date or field 24 data are not implemented; positions 85–96 stay blank.
- SEP reason is copied for 011 only when election type is S or Y, per field 27's enumerated TRCs.
- Submitted uncovered months (186–188), profile cumulative uncovered months (158–160), and profile creditable coverage flag (161) are written only for the acceptance TRC, not for rejection or 134.
- Cumulative months are **not** inferred from submitted months. The returned coverage flag is **not** inferred from the submitted flag because CMS may reset it.
- Relationship choices (510–516) are blank for this catalog. The DTRR layout restricts them to TRCs 394, 395, and 396; those require a later increment.
- UI organization designation is blank for batch inputs.
- The source contract is used as Source ID. Rollover/cleanup, CMS-originated transactions, corrected identities, changed PBPs, and special batch behaviors are outside this version's scenario model.
- Short names use Table 3-17's short-definition wording as test labels where it fits. The PCUG defines field 73 as a 15-character short-name identifier but does not establish an exact machine label for every code here. For 007 and 008, the printed wording exceeds that width, so field 73 is blank rather than an invented truncation. Verify this field against an actual CMS DTRR before asserting exact parity.
- Selected replies are generated first, with acceptance before an informational reply, followed by P. This order is a fixture convention; the consumer should correlate by tracking ID and not assume adjacency/order in actual CMS files.
- All MBIT business fields are preserved as submitted. Choosing 011 for invalid input still generates an acceptance fixture. Full CMS edit validation is not performed.

## Verbatim P mapping

| Field | DTRR positions | Content |
|---|---|---|
| Beneficiary identity | 1–41 | Submitted 1–41 |
| Record type | 42 | P |
| Contract | 43–47 | Submitted 47–51 |
| Submitted transaction text | 48–647 | Exact entire 600-character MBIT detail |
| Accept/reject flag | 648 | A if acceptance selected, otherwise R |
| System tracking ID | 649–668 | Same ID as T records |
| Plan tracking ID | 669–683 | Submitted 210–224 |
| Filler | 684–800 | Spaces |

## Profile fields

Profiles are explicit raw test data, shared across the batch. Field meaning and values must be chosen for the intended scenario. Values are left justified unless the JSON contains leading spaces for numeric/money formatting. Omit a key to leave its field blank.

| JSON key | DTRR positions | Width |
|---|---|---:|
| `DisabilityIndicator` | 53 | 1 |
| `HospiceIndicator` | 54 | 1 |
| `InstitutionalIndicator` | 55 | 1 |
| `EsrdIndicator` | 56 | 1 |
| `EntitlementTypeCode` | 62 | 1 |
| `WorkingAgedIndicator` | 71 | 1 |
| `PreviousPartDContractPbp` | 100–107 | 8 |
| `PriorPbp` | 121–123 | 3 |
| `OutOfAreaFlag` | 134 | 1 |
| `PartCPremium` | 138–145 | 8 |
| `PartDPremium` | 146–153 | 8 |
| `PremiumWithholdOption` | 157 | 1 |
| `CumulativeUncoveredMonths` | 158–160 | 3 |
| `CreditableCoverageFlag` | 161 | 1 |
| `LipsLevel` | 235–237 | 3 |
| `LowIncomeCopayCategory` | 238 | 1 |
| `LowIncomePeriodEffectiveDate` | 239–246 | 8 |
| `LateEnrollmentPenalty` | 247–254 | 8 |
| `LowIncomePremiumSubsidy` | 271–278 | 8 |
| `MspStatusFlag` | 354 | 1 |

LIS fields can be used as explicit fixture context, but this version does not create LIS maintenance records, profiles over time, or TRC 121. A profile can intentionally represent unusual or inconsistent values; only unknown keys, null values, width overflow, and non-ASCII are rejected.
