using HastaneRandevuSistemi.Models;
using HastaneRandevuSistemi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HastaneRandevuSistemi.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly string _jwtSecret;

        public AuthApiController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtIssuer = configuration["Jwt:Issuer"] ?? "HastaneRandevuSistemi";
            _jwtAudience = configuration["Jwt:Audience"] ?? "HastaneRandevuSistemi";
            _jwtSecret = configuration["Jwt:Key"] ?? "HastaneRandevuSistemiSuperSecretKeyDahaUzunOlmali123!";
        }

        [AllowAnonymous]
        [HttpPost("token")]
        public async Task<IActionResult> CreateToken([FromBody] ApiTokenRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanici bilgileri gecersiz." });
            }

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!signInResult.Succeeded)
            {
                return Unauthorized(new { message = "Kullanici bilgileri gecersiz." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddHours(8);

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(new
            {
                accessToken = tokenValue,
                tokenType = "Bearer",
                expiresAtUtc = expiresAt,
                roles
            });
        }
    }
}
