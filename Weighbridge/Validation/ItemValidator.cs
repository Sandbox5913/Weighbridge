using FluentValidation;
using Weighbridge.Models;

namespace Weighbridge.Validation
{
    public class ItemValidator : AbstractValidator<Item>
    {
        public ItemValidator()
        {
            RuleFor(item => item.Name)
                .NotEmpty().WithMessage("Material Name cannot be empty.")
                .MaximumLength(100).WithMessage("Material Name cannot exceed 100 characters.");
        }
    }
}
