using FluentValidation;
using HastaneRandevuSistemi.ViewModels;

namespace HastaneRandevuSistemi.Validators
{
    public class ChangePasswordViewModelValidator : AbstractValidator<ChangePasswordViewModel>
    {
        public ChangePasswordViewModelValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty();

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Yeni sifre en az bir buyuk harf icermelidir.")
                .Matches("[a-z]").WithMessage("Yeni sifre en az bir kucuk harf icermelidir.")
                .Matches("[0-9]").WithMessage("Yeni sifre en az bir rakam icermelidir.")
                .NotEqual(x => x.CurrentPassword).WithMessage("Yeni sifre mevcut sifre ile ayni olamaz.");

            RuleFor(x => x.ConfirmNewPassword)
                .Equal(x => x.NewPassword)
                .WithMessage("Yeni sifre tekrar alani uyusmuyor.");
        }
    }
}
