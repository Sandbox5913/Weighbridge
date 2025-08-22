using FluentValidation;
using Weighbridge.Models;

namespace Weighbridge.Validation
{
    public class SiteValidator : AbstractValidator<Site>
    {
        public SiteValidator()
        {
            RuleFor(site => site.Name)
                .NotEmpty().WithMessage("Site Name cannot be empty.")
                .MaximumLength(100).WithMessage("Site Name cannot exceed 100 characters.");
        }
    }
}
