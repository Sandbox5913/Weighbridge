using FluentValidation;
using Weighbridge.Models;

namespace Weighbridge.Validation
{
    public class TransportValidator : AbstractValidator<Transport>
    {
        public TransportValidator()
        {
            RuleFor(transport => transport.Name)
                .NotEmpty().WithMessage("Transport Name cannot be empty.")
                .MaximumLength(100).WithMessage("Transport Name cannot exceed 100 characters.");
        }
    }
}
