using HastaneRandevuSistemi.Validators;
using HastaneRandevuSistemi.ViewModels;
using Xunit;

namespace HastaneRandevuSistemi.Tests
{
    public class ChangePasswordViewModelValidatorTests
    {
        private readonly ChangePasswordViewModelValidator _validator = new();

        [Fact]
        public void Should_Fail_When_NewPassword_Equals_CurrentPassword()
        {
            var model = new ChangePasswordViewModel
            {
                CurrentPassword = "StrongPass1",
                NewPassword = "StrongPass1",
                ConfirmNewPassword = "StrongPass1"
            };

            var result = _validator.Validate(model);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Should_Pass_When_NewPassword_Is_Strong_And_Different()
        {
            var model = new ChangePasswordViewModel
            {
                CurrentPassword = "StrongPass1",
                NewPassword = "Different2Pass",
                ConfirmNewPassword = "Different2Pass"
            };

            var result = _validator.Validate(model);

            Assert.True(result.IsValid);
        }
    }
}
