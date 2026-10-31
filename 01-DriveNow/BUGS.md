# Bug Reports & Fixes (Task 10)
- **Name**: Ammar Yasser
- **Group / ID**: Backend .NET - Cycle 1

| # | Rule | Failing Test | Expected | Actual | Fix Commit |
|---|---|---|---|---|---|
| 1 | BR-03 | `CalculateRentalDays_ExactlySixtyLeftoverMinutes_ShouldNotAddExtraDay` | 1 day (grace period allows up to 60 leftover minutes without charging extra day) | 2 days (`remainder.TotalMinutes >= 60` counted 60 mins as an extra day) | `ad7475d` |
| 2 | BR-33 | `Calculate_SevenDayRental_AppliesTenPercentDurationDiscount` | 700.00 EGP (10% duration discount on 7,000 base for 7-day rental) | 0.00 EGP (`days > 7 && days < 14` omitted day 7 from discount) | `c3c3fc3` |
| 3 | BR-37 | `Calculate_DiscountCapExceeded_ReducesPromoNotLoyalty` | Promo reduced to 450.00 EGP (15%) keeping loyalty intact at 300.00 EGP (10%) to cap total discount at 25% | Promo uncapped at 600.00 EGP (20%) because cap logic only capped loyalty and ignored promo | `cfe979a` |
| 4 | BR-39 | `Calculate_SilverCustomerWithOneDriver_DoesNotGetFreeDriver` | 100.00 EGP (Silver pays full for driver; only Gold and Platinum get first driver free) | 0.00 EGP (`tier >= LoyaltyTier.Silver` gave Silver free driver) | `362dfc7` |
| 5 | BR-21 | `CreateBooking_BackToBackRental_ShouldBeAllowed` | Succeeded without exception (back-to-back pickup equal to existing return is permitted) | Threw `BookingRejectedException: Vehicle is already booked for these dates` (`<=` and `>=` treated boundary as overlap) | `3cc28c1` |
| 6 | BR-41 | `ProcessReturn_ExactlyTwoHundredFortyMinutesLate_ChargesHourlyNotDaily` | 240.00 EGP (4 started hours @ 10% daily rate = 40% of 600) | 600.00 EGP (`lateMinutes >= 240` charged 100% daily rate at 240 minutes) | `7e409ba` |
| 7 | BR-51 | `Cancel_ExactlyFortyEightHoursNotice_ShouldChargeZeroFee` | 0.00 EGP (notice >= 48 hours incurs 0 fee) | 300.00 EGP (`hours > 48` charged 50% fee at exactly 48 hours) | `ae95f3f` |

---

## Detailed Bug Analysis

### Bug 1 (BR-03): Grace Period Off-By-One
- **File**: `DriveNow.Domain/Services/RentalMathService.cs` (Line 19)
- **Root Cause**: Condition was `remainder.TotalMinutes >= 60`. The business rule states that an extra day is charged only when leftover time strictly exceeds the 1-hour grace period (`> 60`).
- **Fix**: Changed `>= 60` to `> 60`.

### Bug 2 (BR-33): 7-Day Duration Discount Boundary
- **File**: `DriveNow.Application/Services/PricingService.cs` (Line 30)
- **Root Cause**: Condition was `days > 7 && days < 14`. A 7-day rental received 0% discount instead of the required 10%.
- **Fix**: Changed `days > 7` to `days >= 7`.

### Bug 3 (BR-37): Discount Cap Reduction Precedence
- **File**: `DriveNow.Application/Services/PricingService.cs` (Lines 38-46)
- **Root Cause**: The cap logic only capped duration + loyalty against 25%, and added promo discount uncapped. Rule BR-37 specifies that discounts combine additively up to 25%, keeping duration and loyalty intact and reducing the promo discount when the combined total exceeds 25%.
- **Fix**: Kept duration and loyalty, computed raw promo discount, and capped promo discount so that `combinedFixedDiscount + promoDiscount <= 25% of base`.

### Bug 4 (BR-39): Silver Customer Additional Driver Privilege
- **File**: `DriveNow.Application/Services/PricingService.cs` (Line 117)
- **Root Cause**: Driver cost check used `tier >= LoyaltyTier.Silver`. The business rule grants the first additional driver free only to Gold and Platinum tiers.
- **Fix**: Changed `LoyaltyTier.Silver` to `LoyaltyTier.Gold`.

### Bug 5 (BR-21): Back-to-Back Booking Overlap Boundary
- **File**: `DriveNow.Application/Services/BookingService.cs` (Line 66)
- **Root Cause**: Overlap condition checked `request.PickupAt <= existing.ReturnAt && request.ReturnAt >= existing.PickupAt`. This rejected back-to-back rentals where `new PickupAt == existing ReturnAt`.
- **Fix**: Replaced `<=` and `>=` with strict `<` and `>` comparisons.

### Bug 6 (BR-41): Return Late Fee Boundary at 240 Minutes
- **File**: `DriveNow.Application/Services/ReturnService.cs` (Line 131)
- **Root Cause**: Condition was `lateMinutes >= 240`, which escalated to a full daily rate at exactly 240 minutes instead of charging hourly rate (4 started hours = 40%).
- **Fix**: Changed `>= 240` to `> 240`.

### Bug 7 (BR-51): Cancellation Notice Boundary at 48 Hours
- **File**: `DriveNow.Application/Services/CancellationService.cs` (Line 60)
- **Root Cause**: Condition was `hours > 48`. A cancellation requested at exactly 48 hours notice was charged 50% of the daily rate instead of 0 fee.
- **Fix**: Changed `hours > 48` to `hours >= 48`.

---

## Worked Example B Verification

With bugs 2, 3, and 4 fixed:
- **Scenario**: 7-day SUV rental (Thu 09:00 to Thu 09:30 = 7 days + 30 min grace), 1,500 EGP/day, Silver customer aged 23, Premium insurance, 1 additional driver, promo `SAVE15`.
- **Breakdown**:
  - Base Amount: `7 * 1,500 = 10,500.00 EGP`
  - Weekend Surcharge: 2 days (Fri, Sat) * 1,500 * 20% = `600.00 EGP`
  - Young Driver Surcharge: 15% of 10,500 = `1,575.00 EGP`
  - Duration Discount: 10% of 10,500 = `1,050.00 EGP` (BR-33 fixed)
  - Loyalty Discount: 5% of 10,500 = `525.00 EGP`
  - Promo Discount: Raw 15% (1,575) capped to 10% = `1,050.00 EGP` so total discount = 25% (2,625) (BR-37 fixed)
  - Insurance: 300/day * 7 days = `2,100.00 EGP`
  - Drivers: 1 driver * 100/day * 7 days = `700.00 EGP` (Silver pays full, BR-39 fixed)
  - **Total**: `10,500 + 600 + 1,575 - 1,050 - 525 - 1,050 + 2,100 + 700 = 12,850.00 EGP`
- **Test**: `PricingServiceTests.Calculate_WorkedExampleB_ReturnsAccuratePriceBreakdown` passes and confirms `12,850.00 EGP`.
