using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HastaneRandevuSistemi.Models
{
    public class AppUser : IdentityUser
    {
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(50)]
        public string? Surname { get; set; }

        [StringLength(11)]
        public string? TC { get; set; }

        [Phone]
        [StringLength(15)]
        public string? Telefon { get; set; }

        public DateTime? DogumTarihi { get; set; }

        [StringLength(20)]
        public string? Cinsiyet { get; set; }

        [StringLength(250)]
        public string? Adres { get; set; }
    }
}
