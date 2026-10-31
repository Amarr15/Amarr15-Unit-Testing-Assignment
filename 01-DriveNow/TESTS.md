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

### 1. Self Assessment (Step 2)

#### Rule Coverage Table:
| Rule | Test Name | Boundaries Tested | Gap / Notes |
|---|---|---|---|
| **BR-10** (Customer exists) | `CreateBooking_CustomerDoesNotExist_ThrowsCustomerNotFoundException` | Non-existent customer ID | Throws `CustomerNotFoundException`. |
| **BR-11** (Blacklisted customer) | `CreateBooking_CustomerBlacklisted_ThrowsBookingRejectedException` | Customer with `IsBlacklisted = true` | Throws `BookingRejectedException`. |
| **BR-12** (Driver age >= 21) | `CreateBooking_CustomizedAdultCustomer_SucceedsWithValidDates` | Age 30 adult | Age boundary 20 vs 21 covered in AI suggestions. |
| **BR-14** (Vehicle exists) | `CreateBooking_VehicleDoesNotExist_ThrowsVehicleNotFoundException` | Non-existent vehicle ID | Throws `VehicleNotFoundException`. |
| **BR-15** (Vehicle available) | `CreateBooking_VehicleUnavailable_ThrowsBookingRejectedException` | Vehicle with `IsAvailable = false` | Throws `BookingRejectedException`. |
| **BR-18** (Pickup >= 2 hours) | `CreateBooking_PickupTooSoon_ThrowsBookingRejectedException` | Pickup 90 minutes from now (< 2 hours) | Throws `BookingRejectedException`. |
| **BR-19** (Rental span <= 30 days) | `CreateBooking_DurationOverThirtyDays_ThrowsBookingRejectedException` | Span of 35 days | Throws `BookingRejectedException`. |
| **BR-20** (Additional drivers 0..3) | `CreateBooking_TooManyDrivers_ThrowsBookingRejectedException` | 4 additional drivers | Throws `BookingRejectedException`. |
| **BR-21** (No vehicle date overlap) | `CreateBooking_VehicleDatesOverlap_ThrowsBookingRejectedException` | Overlapping date ranges | Throws `BookingRejectedException`. |
| **BR-22** (Customer max 2 bookings) | `CreateBooking_CustomerHasTwoActiveBookings_ThrowsBookingRejectedException` | Customer with 2 active/reserved bookings | Throws `BookingRejectedException`. |
| **BR-24** (Status Reserved & Created) | `CreateBooking_ValidRequest_StoresBookingInFakeRepository`<br>`CreateBooking_ValidRequest_MatchesSnapshotOutput` | Verified in FakeBookingRepo and Verify Snapshot | Status Reserved, correct start mileage. |
| **BR-26** (Deposit & Gateway) | `CreateBooking_StandardCustomer_ChargesDepositViaMockGateway`<br>`CreateBooking_PlatinumCustomer_NeverCallsPaymentGateway`<br>`CreateBooking_MoqStandardCustomer_ChargesDepositOnce`<br>`CreateBooking_MoqPlatinumCustomer_NeverChargesDeposit`<br>`CreateBooking_AutoMoqFrozenGateway_ChargesExpectedDeposit` | Standard customer (charges deposit) vs Platinum customer (0 deposit, gateway not called) | Tested with hand-written Mock, Moq, and AutoMoq. |
| **BR-27** (Confirmation & Audit) | `CreateBooking_ValidRequest_SendsConfirmationSms` | SMS sent with reference and phone number | Tested with Spy double. |

**Verdict**:
I covered the essential business rules for customer/vehicle validation, date constraints, overlap detection, deposit charges with test doubles (Mock/Moq/AutoMoq), and confirmation notifications.

---

### 2. AI Review & Decisions (Steps 3 & 4)

Prompt submitted with BR-10 to BR-27 and test names only (see `screenshots/ai-booking.png`).

| AI Suggestion | Status | Why |
|---|---|---|
| Add test for license minimum duration < 1 year (BR-13). | **Accepted** | Valid gap; added `CreateBooking_LicenseUnderOneYear_ThrowsBookingRejectedException`. |
| Add test for luxury vehicle category restrictions (BR-16). | **Accepted** | Valid gap; added `CreateBooking_LuxuryUnderAgeTwentyFive_ThrowsBookingRejectedException`. |
| Add test for van category license restrictions (BR-17). | **Accepted** | Valid gap; added `CreateBooking_VanLicenseUnderTwoYears_ThrowsBookingRejectedException`. |
| Add test for deposit halved with Premium insurance (BR-26). | **Accepted** | Valid gap; added `CreateBooking_PremiumInsurance_HalvesRequiredDeposit`. |
| Add test for back-to-back rental boundary (BR-21). | **Rejected** (Deferred to Task 10) | Testing back-to-back rentals where `new.PickupAt == existing.ReturnAt` fails against starter code because line 67 uses `<=` instead of `<`. This is an existing bug that will be exposed in Task 10. Added non-overlapping test instead. |
| Pointed out redundancy between hand-written Mock and Moq tests. | **Rejected** | The redundancy is intentional and directly required by Task 6 Requirement R2 to compare hand-written doubles to Moq. |

---

### 3. Judge the AI (Step 5)

The AI accurately caught missing negative tests for license age (BR-13), luxury vehicle rules (BR-16), van rules (BR-17), and deposit reduction under Premium insurance (BR-26). However, the AI incorrectly considered the hand-written mock vs. Moq tests redundant, failing to realize that rewriting hand-written doubles with Moq was an explicit requirement of Task 6 (R2). Additionally, its suggestion to test back-to-back bookings currently fails against starter code due to a bug in the date overlap condition.

---

## Feature: Pickups, Returns, and Cancellations (Task 7)

### 1. My Tests (Self-Assessment)

| Business Rule | Test Name | Scenarios Covered | Expected Behavior |
|---|---|---|---|
| **BR-28** (Pickup window & state) | `StartRental_MoreThanOneHourBeforePickup_ThrowsInvalidBookingStateException`<br>`StartRental_WithinOneHourOfPickup_StartsRentalSuccessfully`<br>`StartRental_NonReservedBooking_ThrowsInvalidBookingStateException` | Too early (> 1h before pickup); valid pickup window (45 min before); non-reserved booking | Rejects early start; sets status to Active; rejects non-reserved. |
| **BR-41** (Late return fee) | `ProcessReturn_LateWithinGracePeriod_ChargesNoLateFee`<br>`ProcessReturn_LateBetweenOneAndFourHours_ChargesHourlyRate`<br>`ProcessReturn_LateOverFourHours_ChargesFullDailyRatePerPeriod` | 0m, 59m (grace period); 60m, 61m, 180m (10% daily rate/hr); 241m, 1440m, 1441m (daily rate/24h period) | 0 EGP for grace; 10% per started hour; full daily rate per started 24h. |
| **BR-42** (Mileage / distance fee) | `ProcessReturn_ExtraMileageDriven_CalculatesDistanceFee` | Within 250 km/day allowance; excess on Economy (0.75 EGP/km); excess on Luxury (1.50 EGP/km) | 0 EGP if within allowance; correct rate per extra km. |
| **BR-43** (Fuel missing quarters fee) | `ProcessReturn_MissingFuelQuarters_CalculatesFuelFee` | Full fuel (4/4); 2 missing quarters on Economy (200 EGP/qtr); 3 missing quarters on SUV (300 EGP/qtr) | 0 EGP; 400 EGP; 900 EGP. |
| **BR-44** (Damage capped at deductible) | `ProcessReturn_ReportedDamage_CapsChargeAtInsuranceDeductible` | Basic under deductible; Basic over 10k deductible; Standard over 3k deductible; Premium (0 deductible) | Charges reported damage capped at deductible per plan. |
| **BR-46** (Return input validations) | `ProcessReturn_EndMileageBelowStart_ThrowsArgumentException`<br>`ProcessReturn_InvalidFuelQuarters_ThrowsArgumentException` | End mileage < start mileage; fuel quarters > 4 | Throws ArgumentException with descriptive error messages. |
| **BR-50** (Cancellation eligibility) | `Cancel_PickupAlreadyPassed_ThrowsInvalidBookingStateException`<br>`Cancel_NonReservedBooking_ThrowsInvalidBookingStateException` | Pickup already passed in time; booking already Active | Throws InvalidBookingStateException. |
| **BR-51** (Cancellation fee tiers) | `Cancel_NoticeMoreThanFortyEightHours_ChargesZeroFee`<br>`Cancel_NoticeBetweenTwentyFourAndFortyEightHours_ChargesFiftyPercentFee`<br>`Cancel_NoticeUnderTwentyFourHours_ChargesFullDailyRate`<br>`Cancel_PlatinumCustomerAnyNotice_NeverChargesFee` | > 48h notice (0 fee); 24h..48h notice (50%); < 24h notice (100%); Platinum customer (always 0 fee) | Charges correct fee percentage; Platinum always exempt. |
| **BR-52** (Fee vs deposit settlement) | `Cancel_FeeExceedsDeposit_ChargesDifferenceViaGateway` | Cancellation fee (1000 EGP) > deposit (400 EGP) | Charges remaining difference (600 EGP) via IPaymentGateway. |

**Verdict**:
Covered core operational lifecycle rules: starting rentals within the allowed 1-hour window, return calculations (grace period, hourly late fee, daily late fee, mileage overage, fuel quarters, damage caps), cancellation fees across notice tiers, and payment gateway charge on deficit.

---

### 2. AI Review & Decisions (Steps 3 & 4)

Prompt submitted with BR-28, BR-41 to BR-46, BR-50 to BR-53 and test names only (see `screenshots/ai-operations.png`).

| AI Suggestion | Status | Why |
|---|---|---|
| Add test for return deposit refund & extra charges reconciliation (BR-45). | **Accepted** | Valid gap; added `ProcessReturn_DepositAndExtras_ChargesExtrasAndRefundsDeposit`. |
| Add test for successful return state changes & mileage update (BR-46). | **Accepted** | Valid gap; added `ProcessReturn_Completed_SetsBookingStatusCompletedAndUpdatesMileage`. |
| Add test for deposit refund on cancellation when deposit > fee (BR-52). | **Accepted** | Valid gap; added `Cancel_DepositExceedsFee_RefundsDifferenceViaGateway`. |
| Add test for promo code usage decrement on cancellation (BR-53). | **Accepted** | Valid gap; added `Cancel_WithPromoCode_DecrementsPromoUsedCount`. |
| Add test for notifications (SMS/Email) and audit logging on cancellation (BR-53). | **Accepted** | Valid gap; added `Cancel_ValidBooking_SendsNotificationsAndWritesAuditLog`. |
| Add test for exact start rental time boundary: `pickup - 1 hour` (BR-28). | **Accepted** | Good boundary case; added `StartRental_ExactlyOneHourBeforePickup_StartsRentalSuccessfully`. |
| Add test for exact cancellation time boundary: `now == pickup` (BR-50). | **Accepted** | Good boundary case; added `Cancel_AtPickupTime_ThrowsInvalidBookingStateException`. |
| Add test for exact distance allowance boundary: `driven == 250 * days` (BR-42). | **Accepted** | Good boundary case; added `ProcessReturn_ExactlyAtAllowedMileage_ChargesZeroDistanceFee`. |
| Add test for zero damage reported (BR-44). | **Accepted** | Good edge case; added `ProcessReturn_ZeroDamageReported_ChargesZeroDamage`. |
| Add test for exact 240-minute late fee boundary (BR-41). | **Rejected** (Deferred to Task 10) | Testing at exactly 240m fails against starter code because `ReturnService.cs` line 125 uses `>= 240` instead of `> 240`, prematurely charging full daily period. This is an existing bug that will be exposed in Task 10. |
| Add test for exact 48-hour notice cancellation fee boundary (BR-51). | **Rejected** (Deferred to Task 10) | Testing at exactly 48h notice fails against starter code because `CancellationService.cs` line 60 uses `> 48` instead of `>= 48`, erroneously charging a 50% fee. This bug will be exposed in Task 10. |
| Consolidate cancellation notice tests into a parameterized matrix. | **Rejected** | Keeping distinct, focused tests for each fee tier makes test reporting and failure diagnostics much cleaner and directly traceable to specific business rule clauses. |

---

### 3. Judge the AI (Step 5)

The AI provided a very sharp and comprehensive review. It correctly spotted that we were missing side-effect tests on return and cancellation (updating booking status, promo code decrement, notifications, and deposit refunds under BR-45, BR-52, and BR-53). It also suggested several valuable boundary tests (such as exactly 1 hour before pickup for starting rental, exact allowed mileage, and cancellation right at pickup time). However, its suggestions to test exact boundaries for 240 minutes late fee and 48 hours cancellation notice uncovered pre-existing bugs in the starter code, which we rightly deferred to Task 10 so as not to break the starter codebase before the designated bug-fixing phase.


