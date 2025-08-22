using FluentValidation;
using Weighbridge.Models;

namespace Weighbridge.Validation
{
    public class UserPageAccessValidator : AbstractValidator<UserPageAccess>
    {
        public UserPageAccessValidator()
        {
            RuleFor(upa => upa.PageName)
                .NotEmpty().WithMessage("Page Name cannot be empty.");
            RuleFor(upa => upa.UserId)
                .GreaterThan(0).WithMessage("User ID must be greater than 0.");
        }
    }
}
