using FluentValidation;
using HastaneRandevuSistemi.ViewModels;

namespace HastaneRandevuSistemi.Validators
{
    public class PatientMedicalHistoryViewModelValidator : AbstractValidator<PatientMedicalHistoryViewModel>
    {
        public PatientMedicalHistoryViewModelValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(120);

            RuleFor(x => x.Diagnosis)
                .MaximumLength(250);

            RuleFor(x => x.Medications)
                .MaximumLength(500);

            RuleFor(x => x.AllergyInfo)
                .MaximumLength(500);

            RuleFor(x => x.Notes)
                .MaximumLength(2000);

            RuleFor(x => x.VisitDate)
                .Must(d => d <= DateTime.Today.AddDays(1))
                .WithMessage("Muayene tarihi gelecekte olamaz.");
        }
    }
}
