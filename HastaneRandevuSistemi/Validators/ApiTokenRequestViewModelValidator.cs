using FluentValidation;
using HastaneRandevuSistemi.ViewModels;

namespace HastaneRandevuSistemi.Validators
{
    public class ApiTokenRequestViewModelValidator : AbstractValidator<ApiTokenRequestViewModel>
    {
        public ApiTokenRequestViewModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }
}
