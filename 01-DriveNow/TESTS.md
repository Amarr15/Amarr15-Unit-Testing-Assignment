# Self-Review Loop (TESTS.md)
- **Name**: Ammar Yasser
- **Group / ID**: Backend .NET - Cycle 1

---

## Feature: RentalMath (Task 1)

### 1. Self Assessment (Step 2)

#### Rule Coverage Table:
| Rule | Test Name | Boundaries Tested | Gap / Notes |
|---|---|---|---|
| **BR-01** (Age) | `CalculateAge_BirthdayHasOccurred_ReturnsCorrectAge`<br>`CalculateAge_BirthdayHasNotOccurred_ReturnsPreviousAge`<br>`CalculateAge_LeapYearBirthday_ReturnsCorrectAge` | Birthday day before vs on the day; Feb 27 vs Feb 28 on non-leap year (2023). | None. |
| **BR-02** (License Years) | `CalculateLicenseYears_DifferentIssueDates_ReturnsExpectedYears` | Issue date before vs after target date. | Mirrors BR-01 logic. |
| **BR-03** (Rental Days) | `CalculateRentalDays_ReturnAtIsBeforeOrEqualPickup_ThrowsArgumentException`<br>`CalculateRentalDays_ValidDuration_ReturnsExpectedDays` | 0 hours, -1 hours for error;<br>3h (min 1 day);<br>24h 00m;<br>24h 59m;<br>25h 01m;<br>72h 00m. | Leftover exactly 60m (25h 00m) is omitted here because production code contains a bug (`>= 60` instead of `> 60`), which will be handled in Task 10. |

**Verdict**:
I covered the main positive paths, exceptions, leap year rules, and duration boundaries. The missing boundary is exactly 60 minutes leftover which exposes a production bug.

---

### 2. AI Review & Decisions (Steps 3 & 4)

Prompt submitted with BR rules and test names only (see `screenshots/ai-rental-math.png`).

| AI Suggestion | Status | Why |
|---|---|---|
| Add a test for Feb 29 birthday in a leap year (e.g. 2024 on Feb 29). | **Accepted** | Valid boundary to verify that leap year birthdays are evaluated on the 29th when the target year is a leap year. |
| Add a test for 25h 00m (exactly 60 minutes leftover). | **Rejected** (Deferred to Task 10) | This is an existing bug in production code. According to the assignment protocol, bugs must be exposed and fixed under Task 10. |
| Add tests for negative age or dates before 1900. | **Rejected** | Not part of the business rules or domain requirements for car rental customers. |

---

### 3. Judge the AI (Step 5)

The AI suggested checking edge cases for centuries (e.g. year 1900 not being a leap year), which is irrelevant for car rental drivers whose birth dates are within the past 18 to 80 years. However, it correctly pointed out testing leap year birthdays when the current year is also a leap year.

---

## Feature: PricingService (Task 5)

### 1. Self Assessment (Step 2)

#### Rule Coverage Table:
| Rule | Test Name | Boundaries Tested | Gap / Notes |
|---|---|---|---|
| **BR-30** (Base Amount) | `Calculate_ValidDuration_ComputesBaseAmount` | 1, 3, 5 rental days at 600 & 1000 daily rates | None. |
| **BR-31** (Weekend Surcharge) | `Calculate_RentalIncludesWeekend_AppliesWeekendSurcharge` | Wed-Thu (0 days), Fri (1 day), Fri-Sat (2 days), Sat-Sun (1 day) | Covered weekend boundary. |
| **BR-32** (Young Driver) | `Calculate_DriverAgeBoundary_AppliesYoungDriverSurchargeCorrectly` | 20 (0%), 21 (15%), 24 (15%), 25 (0%) | Exact boundaries 20/21 and 24/25 covered. |
| **BR-33** (Duration Discount) | `Calculate_RentalDurationDays_AppliesExpectedDurationDiscount` | 6 (0%), 8 (10%), 13 (10%), 14 (15%) | 7 days has a production bug (`> 7` instead of `>= 7`), deferred to Task 10. |
| **BR-34** (Loyalty Discount) | `Calculate_CustomerLoyaltyTier_AppliesExpectedLoyaltyDiscount` | None (0%), Silver (5%), Gold (10%), Platinum (15%) | All tiers covered. |
| **BR-35 / BR-36** (Promo Codes) | `Calculate_InvalidPromoCode_ThrowsExpectedExceptionWithMessage` | Missing code, expired date, too few days, wrong vehicle category, max uses reached | All 5 error messages covered via `[ClassData]`. |
| **BR-37** (25% Discount Cap) | _Pending AI review_ | Cap logic tested implicitly | Need a standalone test for 25% cap combination. |
| **BR-38** (Insurance) | `Calculate_InsurancePlanAndCategory_CalculatesDailyCost` | Basic (0), Standard (150/day), Premium (300/day); Luxury vehicle doubling | All plan and category combinations covered. |
| **BR-39** (Additional Drivers) | `Calculate_CustomerTierWithDrivers_CalculatesDriverCost` | None (no free driver), Gold/Platinum (1st driver free) | Silver customer free driver rule has a bug in production (`>= Silver` instead of Gold/Platinum), deferred to Task 10. |
| **BR-40** (Total Calculation) | `Calculate_WorkedExampleA_ReturnsAccuratePriceBreakdown`<br>`Calculate_WorkedExampleA_MatchesSnapshotOutput` | Full breakdown comparison and Verify snapshot for Worked Example A | Worked Example B deferred to Task 10 due to known bugs in production code. |

#### Bogus Fixed Seed and Test Design (R4):
- **Why a fixed seed matters for FIRST**: In the FIRST principles, tests must be **Repeatable**. When using Bogus without a seed, values are randomly generated every test run. If a random generator picks an unexpected edge-case date (like Feb 29 or weekend), tests can intermittently fail, causing flakiness. With `UseSeed(42)`, the generated objects are 100% deterministic across any machine.
- **Where random data is fine**: Non-functional strings and display fields (like `FullName`, `Email`, `PlateNumber`, `Phone`) that are not inspected by business rules.
- **Where random data is NOT fine**: Boundary fields like `DateOfBirth` (decides the 21–24 young driver surcharge) or `PickupAt` (determines Friday/Saturday weekend surcharge).

#### Snapshot vs Explicit Assertions (R6):
- **When to use Snapshot (Verify)**: Great for verifying complex, multi-field composite objects like `PriceBreakdown` (Example A) or formatted receipt printouts, where checking each field individually is tedious and prone to missing unexpected side effects.
- **When to use Explicit Assertions (FluentAssertions)**: Better for focused unit tests targeting single business rules (e.g., checking only that `WeekendSurcharge` is 240.00), because explicit assertions document the intent and make debugging easy when an isolated failure occurs.

**Verdict**:
I covered the main business rules, individual boundaries, and full Worked Example A with both FluentAssertions and Verify. Known bugs in 7-day duration, Silver free driver, and Worked Example B are identified and deferred to Task 10.

---

### 2. AI Review & Decisions (Steps 3 & 4)

Prompt submitted with BR-30 to BR-40 and test names only (see `screenshots/ai-pricing.png`).

| AI Suggestion | Status | Why |
|---|---|---|
| Add tests for valid promo code application and case-insensitivity (BR-35 / BR-36). | **Accepted** | Valid gap; I previously only tested invalid promo codes throwing exceptions, but never verified that valid uppercase/lowercase codes apply the right discount. |
| Add a test for the 25% discount cap rule (BR-37). | **Accepted** | Added `Calculate_CombinedDiscountsExceedCap_CapsTotalDiscountAtTwentyFivePercent` to verify that when Duration (15%) + Loyalty (15%) exceed 25%, the total is capped at 25%. |
| Add tests for day 7 duration discount boundary (BR-33). | **Rejected** (Deferred to Task 10) | Production code has a bug (`days > 7` instead of `>= 7`). Per assignment rules, bugs are exposed in Task 10. |
| Remove one of the Worked Example A tests due to redundancy. | **Rejected** | The assignment specification explicitly requires both `[MemberData]` full breakdown comparison (R2/R5) and Verify snapshot (R6) for Example A. |

---

### 3. Judge the AI (Step 5)

The AI suggested that having both an explicit assertion test and a snapshot test for Worked Example A is redundant. While theoretically true in everyday development, in the context of this assignment, Requirement R2 explicitly demands `[MemberData]` testing the whole breakdown with `BeEquivalentTo`, and Requirement R6 explicitly demands a Verify snapshot of Example A. The AI also missed that testing the 7-day duration boundary currently fails against the starter code due to an existing bug. However, its suggestion to test case-insensitivity and valid promo code discounts was very helpful and immediately implemented.

---

## Feature: BookingService (Task 6)

## Feature: Pickups, Returns, and Cancellations (Task 7)
