# TDD Log (ParkSmart)
- **Name**: Ammar Yasser
- **Group / ID**: Backend .NET - Cycle 1

## Cycle Log

| Commit | Test Name | Why It Failed (Real failure line) | What You Wrote | Refactor |
|---|---|---|---|---|
| `7428de9` | `Calculate_DurationUpToFifteenMinutes_ReturnsZeroFee` | `System.NotImplementedException: The method or operation is not implemented.` | Initial test setup verifying free 15-minute parking. | None |
| `203a288` | (Green S1) | N/A (Production code for S1) | Implemented `return 0m;` in `ParkingFeeCalculator.Calculate`. | None |
| `c6b204f` | `Calculate_BetweenSixteenAndSixtyMinutes_ChargesTenEgp` | `Expected fee to be 10M, but found 0M.` (Line 45) | Added test for 16-60 mins parking duration. | None |
| `e0065ab` | (Green S2 Cycle 1) | N/A (Production code for S2) | Added check: `if (duration.TotalMinutes > 15) return 10m;` | None |
| `e3e43d4` | `Calculate_SixtyOneMinutes_ChargesTwoHours` | `Expected fee to be 20M, but found 10M.` (Line 61) | Added test for multi-hour parking (61 mins = 2 started hours). | None |
| `6032792` | (Green S2 Cycle 2) | N/A (Production code for S2) | Calculated started hours using `(int)Math.Ceiling(duration.TotalMinutes / 60.0) * 10m`. | None |
| `a105741` | (Refactor S2) | N/A (Refactoring) | Extracted started hours calculation into readable logic. | Refactored started hours calculation |
| `2cfe53a` | `Calculate_ExitBeforeEntry_ThrowsArgumentException` | `Expected a <System.ArgumentException> to be thrown, but no exception was thrown.` (Line 77) | Added test for invalid session where exit time precedes entry time. | None |
| `a883d6a` | (Green S3) | N/A (Production code for S3) | Added validation guard: `if (session.ExitTime < session.EntryTime) throw new ArgumentException(...)`. | None |
| `f15f5d0` | `Calculate_LongDuration_AppliesDailyCapOfEightyPerTwentyFourHours` | `Expected fee to be 80M, but found 100M (difference of 20M).` (Line 98) | Added tests for daily cap (10h = 80, 30h = 140, 48h = 160, 48h10m = 170). | None |
| `6cc6f89` | (Green S4) | N/A (Production code for S4) | Split total duration into full 24-hour days (`duration.TotalHours / 24 * 80m`) plus remaining hours capped at 80m. | None |
| `2f92b0b` | `Calculate_VehicleType_AppliesTypeSpecificHourlyRateAndDailyCap` | `Expected fee to be 15M, but found 30M.` (Line 118) | Added tests for Motorcycle (5/hr, cap 40) and Truck (25/hr, cap 200). | None |
| `87b8e94` | (Green S5) | N/A (Production code for S5) | Added vehicle rates mapping returning specific hourly rate and daily cap. | None |
| `268f92f` | `Calculate_NightWindow_AppliesLowerOfNormalFeeAndNightFlatRate` | `Expected fee to be 30M, but found 80M.` (Line 140) | Added tests for night window (22:00 to 08:00 next day) with vehicle night flat rates. | None |
| `29a5834` | (Green S6) | N/A (Production code for S6) | Checked if entry/exit falls in night window and capped fee at night flat rate if lower. | None |
| `b0d0caa` | (Refactor S6) | N/A (Refactoring) | Extracted `IsNightWindow` and `GetVehicleRates` helper methods for clean separation. | Refactored helper methods |
| `c897107` | `Calculate_TicketLost_AppliesMinimumFeeAndBypassesFreePeriod` | `Expected fee to be 150M, but found 0M.` (Line 162) | Added tests for lost ticket: bypasses grace period and charges at least minimum lost ticket fee. | None |
| `54b8afe` | (Green S7) | N/A (Production code for S7) | Conditioned grace period on `!session.TicketLost` and applied `Math.Max(fee, lostTicketMin)`. | None |
| `6c15264` | `Calculate_FridayEntry_ExtendsFreePeriodToTwoHours` | `Expected fee to be 0M, but found 20M.` (Line 182) | Added tests for Friday entry extending free parking to 2 hours (120 mins). | None |
| `015ef1a` | (Green S8) | N/A (Production code for S8) | Set `freeMinutes = session.EntryTime.DayOfWeek == DayOfWeek.Friday ? 120 : 15;`. | None |
| `caaee1c` | `Calculate_Subscriber_AppliesFreeWindowOrFiftyPercentDiscountAndAppliesLostTicketMinimumAfterwards` | `Expected fee to be 0M, but found 80M.` (Line 204) | Added tests for subscribers: free in Sun-Thu 07:00-19:00 daytime window, 50% discount outside, applied before lost ticket min. | None |
| `f0dd61d` | (Green S9) | N/A (Production code for S9) | Implemented `IsSubscriberFreeWindow` and 50% rounding discount before lost ticket check. | None |
| `9944aa2` | (Refactor S9) | N/A (Refactoring) | Extracted `ApplySubscriberDiscount` helper method to simplify fee pipeline. | Extracted subscriber fee adjustment helper |
| `0409374` | `Calculate_NegativeChargedKwh_ThrowsArgumentException` | `Expected ArgumentException to be thrown, but no exception was thrown.` (Line 220) | Added test for negative kWh throwing ArgumentException and EV charging fee additions without cap/discount. | None |
| `2999962` | (Green S10) | N/A (Production code for S10) | Added check for negative `ChargedKwh` and added `Math.Round(session.ChargedKwh * 3.50m, 2)` at the end of calculation. | None |
| `5b1424e` | (Refactor S10) | N/A (Refactoring) | Extracted `CalculateBaseParkingFee` and `CalculateEvChargingFee` for modular and readable fee pipeline. | Cleaned fee calculation pipeline |

## Retrospective

- **Which stage forced the biggest change to earlier code?**  
Stage S4 (Daily Cap) and Stage S6 (Night Flat Rate). Initially in stages S1 and S2, the calculation seemed trivial enough to simply multiply started hours by the hourly rate. However, when S4 introduced the 24-hour block daily cap, the entire time calculation had to be restructured to decompose duration into full 24-hour days plus remaining hours (capping each remainder independently). Then in S6, the night rate required inspecting actual wall-clock hours rather than just a relative duration span.

- **What did that teach you about designing up front?**  
It taught me that trying to anticipate every future requirement upfront usually leads to over-engineering and speculative complexity. Practicing strict TDD (Red -> Green -> Refactor) kept the code lean and focused solely on what was needed to satisfy the current tests. Because refactoring was performed immediately after reaching Green in multiple stages (S2, S6, S9, S10), the code structure remained clean and extensible, allowing subsequent complex rules (such as Friday grace extensions, subscriber daytime windows, and EV charging additions) to be added seamlessly without breaking existing behavior.
