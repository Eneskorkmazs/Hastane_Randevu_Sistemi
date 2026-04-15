using FluentValidation;
using HastaneRandevuSistemi.ViewModels;

namespace HastaneRandevuSistemi.Validators
{
    public class RegisterViewModelValidator : AbstractValidator<RegisterViewModel>
    {
        public RegisterViewModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Surname)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(256);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Sifre en az bir buyuk harf icermelidir.")
                .Matches("[a-z]").WithMessage("Sifre en az bir kucuk harf icermelidir.")
                .Matches("[0-9]").WithMessage("Sifre en az bir rakam icermelidir.");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password)
                .WithMessage("Sifreler uyusmuyor.");

            RuleFor(x => x.TC)
                .NotEmpty()
                .Matches(@"^\d{11}$")
                .WithMessage("TC kimlik numarasi 11 haneli olmalidir.");

            RuleFor(x => x.Telefon)
                .NotEmpty()
                .MaximumLength(15);

            RuleFor(x => x.DogumTarihi)
                .NotNull()
                .Must(d => d <= DateTime.Today)
                .WithMessage("Dogum tarihi bugunden ileri olamaz.");

            RuleFor(x => x.Cinsiyet)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(x => x.Adres)
                .NotEmpty()
                .MaximumLength(250);
        }
    }
}
