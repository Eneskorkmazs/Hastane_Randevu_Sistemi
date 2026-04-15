using HastaneRandevuSistemi.Validators;
using HastaneRandevuSistemi.ViewModels;
using Xunit;

namespace HastaneRandevuSistemi.Tests
{
    public class RegisterViewModelValidatorTests
    {
        private readonly RegisterViewModelValidator _validator = new();

        [Fact]
        public void Should_Fail_When_Password_Is_Weak()
        {
            var model = BuildValidModel();
            model.Password = "abcdefghi";
            model.ConfirmPassword = "abcdefghi";

            var result = _validator.Validate(model);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Should_Pass_When_Model_Is_Valid()
        {
            var model = BuildValidModel();

            var result = _validator.Validate(model);

            Assert.True(result.IsValid);
        }

        private static RegisterViewModel BuildValidModel()
        {
            return new RegisterViewModel
            {
                Name = "Enes",
                Surname = "Korkmaz",
                Email = "enes@example.com",
                Password = "StrongPass1",
                ConfirmPassword = "StrongPass1",
                TC = "12345678901",
                Telefon = "05001234567",
                DogumTarihi = new DateTime(2000, 1, 1),
                Cinsiyet = "Erkek",
                Adres = "Sanliurfa"
            };
        }
    }
}
