using FluentValidation;
using Weighbridge.Models;

namespace Weighbridge.Validation
{
    public class UserValidator : AbstractValidator<User>
    {
        public UserValidator()
        {
            RuleFor(user => user.Username)
                .NotEmpty().WithMessage("Username cannot be empty.")
                .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.");

            // This rule applies when adding a new user or when a new password is provided for an existing user
            RuleFor(user => user.PasswordHash)
                .NotEmpty().When(user => user.Id == 0 || !string.IsNullOrEmpty(user.PasswordHash)) // For new users (Id == 0) or when password is being updated
                .WithMessage("Password cannot be empty.");
        }
    }
}
