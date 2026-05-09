using FluentValidation;
using Homework1.Api.Endpoints;

namespace Homework1.Api.Validators;

internal sealed class CreateTransactionRequestValidator : AbstractValidator<TransactionsEndpoints.CreateTransactionRequest>
{
    private static readonly HashSet<string> ValidIsoCurrencies = new()
    {
        "AED", "AFN", "ALL", "AMD", "ANG", "AOA", "ARS", "AUD", "AWG", "AZN",
        "BAM", "BBD", "BDT", "BGN", "BHD", "BIF", "BMD", "BND", "BOB", "BRL", "BSD", "BTC", "BTN", "BWP", "BYN", "BZD",
        "CAD", "CDF", "CHE", "CHF", "CHW", "CLF", "CLP", "CNH", "CNY", "COP", "COU", "CRC", "CUC", "CUP", "CVE", "CZK",
        "DJF", "DKK", "DOP", "DZD",
        "EGP", "ERN", "ETB", "EUR",
        "FJD", "FKP",
        "GBP", "GEL", "GGP", "GHS", "GIP", "GMD", "GNF", "GTQ", "GYD",
        "HKD", "HNL", "HRK", "HTG", "HUF",
        "IDR", "ILS", "IMP", "INR", "IQD", "IRR", "ISK",
        "JEP", "JMD", "JOD", "JPY",
        "KES", "KGS", "KHR", "KMF", "KPW", "KRW", "KWD", "KYD", "KZT",
        "LAK", "LBP", "LKR", "LRD", "LSL", "LYD",
        "MAD", "MDL", "MGA", "MKD", "MMK", "MNT", "MOP", "MRU", "MUR", "MVR", "MWK", "MXN", "MXV", "MYR", "MZN",
        "NAD", "NGN", "NIO", "NOK", "NPR", "NZD",
        "OMR",
        "PAB", "PEN", "PGK", "PHP", "PKR", "PLN", "PYG",
        "QAR",
        "RON", "RSD", "RUB", "RWF",
        "SAR", "SBD", "SCR", "SDG", "SEK", "SGD", "SHP", "SLL", "SOS", "SOL", "SRD", "SSP", "STN", "SYP", "SZL",
        "THB", "TJS", "TMT", "TND", "TOP", "TRY", "TTD", "TWD", "TZS",
        "UAH", "UGX", "USD", "USN", "UYI", "UYU", "UYW", "UZS",
        "VED", "VES", "VND", "VUV",
        "WST",
        "XAF", "XAG", "XAU", "XBA", "XBB", "XBC", "XBD", "XCD", "XDR", "XOF", "XPD", "XPF", "XPT", "XSU", "XTS", "XUA", "XXX",
        "YER",
        "ZAR", "ZMW", "ZWL"
    };

    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.FromAccount)
            .NotEmpty()
            .Matches(@"^ACC-[A-Z0-9]+$")
            .WithMessage("FromAccount must match the format ACC-[A-Z0-9]+");

        RuleFor(x => x.ToAccount)
            .NotEmpty()
            .Matches(@"^ACC-[A-Z0-9]+$")
            .WithMessage("ToAccount must match the format ACC-[A-Z0-9]+");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .Must(HaveAtMostTwoDecimalPlaces)
            .WithMessage("Amount must have at most 2 decimal places");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(IsValidIsoCurrency)
            .WithMessage("Currency must be a valid ISO 4217 code");
    }

    private static bool HaveAtMostTwoDecimalPlaces(decimal amount)
    {
        decimal scaled = amount * 100;
        return scaled == Math.Floor(scaled);
    }

    private static bool IsValidIsoCurrency(string currency)
    {
        return ValidIsoCurrencies.Contains(currency.ToUpperInvariant());
    }
}
