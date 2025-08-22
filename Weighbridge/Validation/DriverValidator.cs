using FluentValidation;
using Weighbridge.Models;

namespace Weighbridge.Validation
{
    public class DriverValidator : AbstractValidator<Driver>
    {
        public DriverValidator()
        {
            RuleFor(driver => driver.Name)
                .NotEmpty().WithMessage("Driver Name cannot be empty.")
                .MaximumLength(100).WithMessage("Driver Name cannot exceed 100 characters.");
        }
    }
}
