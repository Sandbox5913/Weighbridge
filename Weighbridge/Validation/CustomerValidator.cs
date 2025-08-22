using FluentValidation;
using Weighbridge.Models;

namespace Weighbridge.Validation
{
    public class CustomerValidator : AbstractValidator<Customer>
    {
        public CustomerValidator()
        {
            RuleFor(customer => customer.Name)
                .NotEmpty().WithMessage("Customer Name cannot be empty.")
                .MaximumLength(100).WithMessage("Customer Name cannot exceed 100 characters.");
        }
    }
}
