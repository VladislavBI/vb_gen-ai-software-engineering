using FluentAssertions;
using FluentValidation;
using Homework1.Api.Endpoints;
using Homework1.Api.Validators;

#pragma warning disable IDE0008 // Use explicit type instead of var - relaxed for test code
namespace Homework1.Tests.Api.Validators;

public class CreateTransactionRequestValidatorTests
{
    private readonly CreateTransactionRequestValidator _validator;

    public CreateTransactionRequestValidatorTests()
    {
        _validator = new CreateTransactionRequestValidator();
    }

    #region Amount Validation

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.99)]
    [InlineData(1)]
    [InlineData(100.50)]
    [InlineData(9999.99)]
    public async Task Validate_ValidAmounts_PassesValidation(decimal amount)
    {
        // Arrange
        var request = new TransactionsEndpoints.CreateTransactionRequest(
            "ACC-12345",
            "ACC-67890",
            amount,
            "USD",
            "transfer");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100.50)]
    [InlineData(0)]
    public async Task Validate_NonPositiveAmount_FailsValidation(decimal amount)
    {
        // Arrange
        var request = new TransactionsEndpoints.CreateTransactionRequest(
            "ACC-12345",
            "ACC-67890",
            amount,
            "USD",
            "transfer");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Theory]
    [InlineData(1.111)]
    [InlineData(100.555)]
    [InlineData(0.123)]
    [InlineData(99.999)]
    public async Task Validate_MoreThanTwoDecimalPlaces_FailsValidation(decimal amount)
    {
        // Arrange
        var request = new TransactionsEndpoints.CreateTransactionRequest(
            "ACC-12345",
            "ACC-67890",
            amount,
            "USD",
            "transfer");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount" && e.ErrorMessage.Contains("decimal"));
    }

    #endregion

    #region Account Format Validation

    [Theory]
    [InlineData("ACC-123")]
    [InlineData("ACC-ABC")]
    [InlineData("ACC-ABC123")]
    [InlineData("ACC-ACCOUNT1")]
    [InlineData("ACC-0")]
    public async Task Validate_ValidFromAccountFormat_PassesValidation(string fromAccount)
    {
        // Arrange
        var request = new TransactionsEndpoints.CreateTransactionRequest(
            fromAccount,
            "ACC-67890",
            50,
            "USD",
            "transfer");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue(because: $"fromAccount format {fromAccount} should be valid");
    }

    [Theory]
    [InlineData("acc-123")]
    [InlineData("ACC123")]
    [InlineData("ACCOUNT-123")]
    [InlineData("ACC-123-456")]
    [InlineData("ACC 123")]
    [InlineData("123-ACC")]
    [InlineData("")]
    public async Task Validate_InvalidFromAccountFormat_FailsValidation(string fromAccount)
    {
        // Arrange
        var request = new TransactionsEndpoints.CreateTransactionRequest(
            fromAccount,
            "ACC-67890",
            50,
            "USD",
            "transfer");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FromAccount");
    }

    [Theory]
    [InlineData("ACC-123")]
    [InlineData("ACC-ABC")]
    [InlineData("ACC-ABC123")]
    public async Task Validate_ValidToAccountFormat_PassesValidation(string toAccount)
    {
        // Arrange
        var request = new TransactionsEndpoints.CreateTransactionRequest(
            "ACC-12345",
            toAccount,
            50,
            "USD",
            "transfer");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue(because: $"toAccount format {toAccount} should be valid");
    }

    [Theory]
    [InlineData("acc-123")]
    [InlineData("ACC123")]
    [InlineData("")]
    public async Task Validate_InvalidToAccountFormat_FailsValidation(string toAccount)
    {
        // Arrange
        var request = new TransactionsEndpoints.CreateTransactionRequest(
            "ACC-12345",
            toAccount,
            50,
            "USD",
            "transfer");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ToAccount");
    }

    #endregion

    #region Currency Validation

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("CAD")]
    [InlineData("AUD")]
    public async Task Validate_ValidCurrency_PassesValidation(string currency)
    {
        // Arrange
        var request = new TransactionsEndpoints.CreateTransactionRequest(
            "ACC-12345",
            "ACC-67890",
            50,
            currency,
            "transfer");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue(because: $"currency {currency} should be valid");
    }

    [Theory]
    [InlineData("ZZZ")]
    [InlineData("INVALID")]
    [InlineData("usd")]
    [InlineData("")]
    [InlineData("U")]
    public async Task Validate_InvalidCurrency_FailsValidation(string currency)
    {
        // Arrange
        var request = new TransactionsEndpoints.CreateTransactionRequest(
            "ACC-12345",
            "ACC-67890",
            50,
            currency,
            "transfer");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    #endregion

    #region Combined Validation

    [Fact]
    public async Task Validate_AllFieldsValid_PassesValidation()
    {
        // Arrange
        var request = new TransactionsEndpoints.CreateTransactionRequest(
            "ACC-SENDER",
            "ACC-RECEIVER",
            123.45m,
            "USD",
            "transfer");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_MultipleFieldsInvalid_FailsWithMultipleErrors()
    {
        // Arrange
        var request = new TransactionsEndpoints.CreateTransactionRequest(
            "invalid-account",
            "ACC-RECEIVER",
            -50,
            "INVALID",
            "transfer");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion
}
