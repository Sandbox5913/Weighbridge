using FluentValidation;
using Weighbridge.Models;

namespace Weighbridge.Validation
{
    public class VehicleValidator : AbstractValidator<Vehicle>
    {
        public VehicleValidator()
        {
            RuleFor(vehicle => vehicle.LicenseNumber)
                .NotEmpty().WithMessage("License Number cannot be empty.")
                .MaximumLength(50).WithMessage("License Number cannot exceed 50 characters.");

            RuleFor(vehicle => vehicle.TareWeight)
                .GreaterThanOrEqualTo(0).WithMessage("Tare Weight cannot be negative.");
        }
    }
}
