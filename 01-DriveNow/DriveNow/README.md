# DriveNow

DriveNow is a car-rental system for a rental company in Egypt. Staff use the console to quote prices, book cars, start rentals, take returns and cancel bookings.

## Projects

- `DriveNow.Domain` holds customers, vehicles, bookings, promo codes, price figures and the rental calendar math.
- `DriveNow.Application` prices a rental, creates and starts bookings, takes returns, cancels bookings and builds receipts.
- `DriveNow.Infrastructure` keeps the in-memory lists, talks to the payment gateway, and sends SMS and email.
- `DriveNow.ConsoleApp` is the staff menu.
- `tests/DriveNow.LegacyTests` checks how a receipt is assembled.

## Run

```bash
dotnet run --project src/DriveNow.ConsoleApp
```

## Test

```bash
dotnet test
```

## Business rules

Money is EGP. Weekend days are Friday and Saturday. Money values are rounded with `Math.Round(x, 2, MidpointRounding.AwayFromZero)`.

### Rental calendar

| Id | Rule |
| --- | --- |
| BR-01 | Age is the number of full years on the given date. A birthday that has not happened yet this year does not count. A person born on 29 February has their birthday on 28 February in a non-leap year. |
| BR-02 | License years use the same rule as BR-01, measured from the license issue date. |
| BR-03 | Rental days: the return must be after pickup. Count full 24-hour blocks. If the leftover time is more than 60 minutes, add one day. Exactly 60 minutes does not add a day. The result is at least 1. |

| Span | Days |
| --- | --- |
| 24h 59m | 1 |
| 25h 00m | 1 |
| 25h 01m | 2 |
| 3h | 1 |
| 72h | 3 |

### Pricing

`PricingService.Calculate` rounds every money value.

| Id | Rule |
| --- | --- |
| BR-30 | Rental days come from BR-03. Base = daily rate × rental days. |
| BR-31 | Weekend surcharge = 20% of the daily rate for every rental day that starts on Friday or Saturday. Day k starts at pickup + k × 24 hours (k = 0 .. days − 1). |
| BR-32 | Young driver surcharge = 15% of base when the driver is 21–24 on the pickup date. |
| BR-33 | Duration discount = 10% of base for 7–13 days, 15% for 14 days or more. |
| BR-34 | Loyalty discount = 5% Silver, 10% Gold, 15% Platinum, of base. |
| BR-35 | Promo discount = the promo percent of base. |
| BR-36 | When a promo code is present, lookup is case-insensitive. The first failure wins: missing code; today outside the valid dates (inclusive); rental shorter than the minimum; vehicle category not allowed (`null` means every category); used count already at the maximum. |
| BR-37 | Duration, loyalty and promo percents together never exceed 25% of base. When the cap applies, keep duration and loyalty and reduce the promo discount. |
| BR-38 | Insurance per day: Basic 0, Standard 150, Premium 300. Luxury vehicles pay double. |
| BR-39 | Additional drivers cost 100 per driver per day. Gold and Platinum customers get the first additional driver free. |
| BR-40 | Total = base + weekend + young driver − duration − loyalty − promo + insurance + drivers. |

Promo messages:

| Situation | Message |
| --- | --- |
| Not found | `Promo code '{code}' does not exist` |
| Outside the valid dates | `Promo code '{code}' has expired` |
| Too few days | `Promo code '{code}' requires at least {MinDays} days` |
| Wrong category | `Promo code '{code}' is not valid for {Category}` |
| No uses left | `Promo code '{code}' has been fully used` |

Worked example A. Vehicle 600 per day, Sunday 10:00 to Wednesday 10:00, Gold customer aged 34, Standard insurance, 0 drivers, no promo.

| Field | Amount |
| --- | --- |
| Days | 3 |
| Base | 1,800.00 |
| Weekend | 0.00 |
| Young driver | 0.00 |
| Loyalty | 180.00 |
| Insurance | 450.00 |
| Total | 2070.00 |

Worked example B. Vehicle 1,500 per day (SUV), Thursday 09:00 to the next Thursday 09:30, Silver customer aged 23, promo SAVE15, Premium insurance, 1 driver.

| Field | Amount |
| --- | --- |
| Days | 7 |
| Base | 10,500.00 |
| Weekend | 600.00 |
| Young driver | 1,575.00 |
| Duration | 1,050.00 |
| Loyalty | 525.00 |
| Promo | 1,050.00 (capped from 1,575.00) |
| Insurance | 2,100.00 |
| Drivers | 700.00 |
| Total | 12850.00 |

### Booking

`BookingService.CreateBooking` stops at the first failure. Age and license years are measured on the pickup date.

| Id | Rule |
| --- | --- |
| BR-10 | The customer must exist, or `Customer #{id} not found`. |
| BR-11 | A blacklisted customer is rejected: `Customer is blacklisted`. |
| BR-12 | Age must be at least 21: `Driver must be at least 21`. |
| BR-13 | The license must be at least 1 year old: `License must be held for at least 1 year`. |
| BR-14 | The vehicle must exist, or `Vehicle #{id} not found`. |
| BR-15 | The vehicle must be available: `Vehicle is not available`. |
| BR-16 | Luxury: age at least 25 and license at least 3 years, or `Luxury vehicles require age 25+ and 3 years of license`. |
| BR-17 | Van: license at least 2 years, or `Vans require 2 years of license`. |
| BR-18 | Pickup is at least 2 hours from now: `Pickup must be at least 2 hours from now`. |
| BR-19 | Return is after pickup and the span is at most 30 days: `Rental period is invalid`. |
| BR-20 | Additional drivers are from 0 to 3: `Up to 3 additional drivers allowed`. |
| BR-21 | No overlap with the vehicle's reserved or active bookings. Overlap means the new pickup is before the existing return and the new return is after the existing pickup. Back-to-back (new pickup equal to the existing return) is allowed: `Vehicle is already booked for these dates`. |
| BR-22 | The customer has fewer than 2 open bookings: `Customer already has 2 active bookings`. |
| BR-23 | The price is calculated with `PricingService`, which also checks the promo (BR-36). |
| BR-24 | Status is Reserved, created-at is now, start mileage is the vehicle mileage. |
| BR-25 | Reference is `DN-` + the pickup date `yyyyMMdd` + `-` + 4 letters or digits. |
| BR-26 | Deposit: Economy/Compact 1,000, SUV/Van 2,500, Luxury 5,000. Premium insurance halves it. Platinum customers pay 0 and the gateway is not called. Otherwise the gateway charges `Deposit {reference}`. A gateway failure saves nothing. |
| BR-27 | A used promo increments its used count. The booking is stored. SMS and email say `Booking {reference} confirmed`. The audit log records `Booking {reference} created`. |

`BookingService.StartRental`: the booking must exist (`Booking #{id} not found`) and be Reserved (`Only reserved bookings can be started`). Now must be at least the pickup minus 1 hour (`Too early to start this rental`). Status becomes Active.

### Return

`ReturnService.ProcessReturn` accepts an active booking only (`Only active bookings can be returned`). End mileage below the start mileage is rejected. Fuel quarters must be 0 through 4. The actual return time is now. An early return does not refund unused days (BR-45).

| Id | Rule |
| --- | --- |
| BR-41 | Late minutes are whole minutes after the planned return, or 0 when the car is early. 0–59 minutes: no fee. 60–240 minutes: 10% of the daily rate per started hour (60 minutes = 1 hour, 61 minutes = 2 hours, 240 minutes = 4 hours). More than 240 minutes: the daily rate × the number of started 24-hour periods. |
| BR-42 | Allowed distance is 250 km × rental days. Extra kilometres are 0.75 EGP, or 1.50 EGP for Luxury. |
| BR-43 | Cars leave with a full tank (4 quarters). Each missing quarter is 200 EGP, or 300 EGP for SUV and Van. |
| BR-44 | Damage charged is the smaller of the reported damage and the deductible. Deductible: Basic 10,000, Standard 3,000, Premium 0. |
| BR-46 | Status becomes Completed. The vehicle mileage is updated. The customer's completed rentals increase by 1 and the loyalty tier is upgraded when the new count qualifies; a tier is never lowered. Extras are charged when they are above 0 (`Extras {reference}`). A deposit above 0 is refunded (`Deposit refund {reference}`). The receipt is emailed with subject `Receipt {reference}`. |

### Cancellation

`CancellationService.Cancel` accepts a Reserved booking that has not reached its pickup (`Only reserved bookings can be cancelled`, `Booking has already started`).

| Id | Rule |
| --- | --- |
| BR-51 | Hours until pickup decide the fee. 48 hours or more: 0. From 24 hours up to 48: 50% of the daily rate. Under 24 hours: 100% of the daily rate. Platinum customers never pay a fee. |
| BR-52 | Refund is the deposit minus the fee, and never below 0. The difference is charged when the fee is larger than the deposit. |
| BR-53 | Status becomes Cancelled. A promo used count goes down by 1. SMS and email say `Booking {reference} cancelled`. The method returns the fee. |

### Receipt

Non-zero lines, in order: Base rental, Weekend surcharge, Young driver surcharge, Duration discount (negative), Loyalty discount (negative), Promo discount (negative), Insurance, Additional drivers. After a return, also Late fee, Extra mileage, Fuel, Damage.

VAT is 14% of each line, rounded. Amounts are shown as `#,##0.00` plus ` EGP`. Each line is the description padded to 30 dots, then the amount padded to the widest amount. VAT 14% and Total follow the same shape. The text starts with `DriveNow receipt {reference}`.

## Seed data

Dates below are relative to today.

### Customers

Email is `firstname.lastname@drivenow.eg`. Phone is `0100000000` plus the id.

| Id | Name | Date of birth | License issued | Tier | Completed rentals | Blacklisted |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Ahmed Fawzy | today − 34 years | today − 12 years | Gold | 18 | no |
| 2 | Mariam Adel | today − 23 years | today − 3 years | None | 2 | no |
| 3 | Khaled Mostafa | today − 25 years − 10 days | today − 4 years | Silver | 7 | no |
| 4 | Salma Hany | today − 21 years − 3 days | today − 1 year − 5 days | None | 0 | no |
| 5 | Tamer Nabil | today − 40 years | today − 20 years | Platinum | 35 | no |
| 6 | Rana Sherif | today − 29 years | today − 2 years | Silver | 6 | yes |
| 7 | Omar Wael | today − 20 years | today − 1 year | None | 0 | no |
| 8 | Youssef Kamal | today − 27 years | today − 6 months | None | 0 | no |

### Vehicles

| Id | Plate | Model | Category | Daily rate | Mileage | Available |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | ABC-1001 | Hyundai Accent | Economy | 600 | 42,000 | yes |
| 2 | ABC-1002 | Kia Rio | Economy | 650 | 18,500 | yes |
| 3 | CMP-2001 | Toyota Corolla | Compact | 900 | 30,200 | yes |
| 4 | CMP-2002 | Nissan Sunny | Compact | 850 | 77,000 | no |
| 5 | SUV-3001 | Kia Sportage | SUV | 1,500 | 25,000 | yes |
| 6 | SUV-3002 | Hyundai Tucson | SUV | 1,600 | 12,000 | yes |
| 7 | LUX-4001 | Mercedes C200 | Luxury | 4,000 | 9,000 | yes |
| 8 | LUX-4002 | BMW 520i | Luxury | 4,500 | 15,500 | yes |
| 9 | VAN-5001 | Toyota HiAce | Van | 2,000 | 60,000 | yes |
| 10 | VAN-5002 | Peugeot Expert | Van | 1,800 | 33,000 | yes |

### Promo codes

| Code | Percent | Valid from | Valid until | Min days | Categories | Max uses | Used |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SAVE15 | 15 | today − 10 days | today + 60 days | 1 | all | 100 | 3 |
| WEEKLY20 | 20 | today − 5 days | today + 90 days | 7 | all | 50 | 0 |
| LUX10 | 10 | today − 30 days | today + 30 days | 2 | Luxury | 20 | 20 |
| SUMMER25 | 25 | today − 120 days | today − 30 days | 1 | all | 100 | 40 |

Two bookings are already stored. `DN-SEED-0001` is Ahmed Fawzy (customer 1) on the Kia Sportage (vehicle 5), pickup today + 3 days at 10:00, return today + 6 days at 10:00, Standard insurance, deposit 2,500, status Reserved, start mileage 25,000. `DN-SEED-0002` is Tamer Nabil (customer 5) on the Mercedes C200 (vehicle 7), pickup today − 2 days at 09:00, return today + 1 day at 09:00, Standard insurance, deposit 0, status Active, start mileage 9,000. Both were created today − 7 days, with no promo and no additional drivers. The console prices them when it starts.
