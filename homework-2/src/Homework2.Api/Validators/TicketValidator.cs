using FluentValidation;
using Homework2.Api.Models;

namespace Homework2.Api.Validators;

/// <summary>Validator for CreateTicketRequest.</summary>
internal sealed class CreateTicketValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketValidator()
    {
        _ = RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required");

        _ = RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("CustomerEmail is required")
            .EmailAddress().WithMessage("CustomerEmail must be a valid email address");

        _ = RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("CustomerName is required");

        _ = RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required")
            .MinimumLength(1).WithMessage("Subject must be at least 1 character")
            .MaximumLength(200).WithMessage("Subject must be no more than 200 characters");

        _ = RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters")
            .MaximumLength(2000).WithMessage("Description must be no more than 2000 characters");
    }
}

/// <summary>Validator for UpdateTicketRequest.</summary>
internal sealed class UpdateTicketValidator : AbstractValidator<UpdateTicketRequest>
{
    public UpdateTicketValidator()
    {
        _ = RuleFor(x => x.Subject)
            .MinimumLength(1).WithMessage("Subject must be at least 1 character")
            .MaximumLength(200).WithMessage("Subject must be no more than 200 characters")
            .When(x => x.Subject is not null);

        _ = RuleFor(x => x.Description)
            .MinimumLength(10).WithMessage("Description must be at least 10 characters")
            .MaximumLength(2000).WithMessage("Description must be no more than 2000 characters")
            .When(x => x.Description is not null);
    }
}
