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

## Feature: BookingService (Task 6)

## Feature: Pickups, Returns, and Cancellations (Task 7)
