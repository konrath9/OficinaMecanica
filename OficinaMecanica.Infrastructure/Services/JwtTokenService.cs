using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Infrastructure.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OficinaMecanica.Infrastructure.Services
{
    public class JwtTokenService : ITokenService
    {
        private readonly JwtSettings _settings;

        public JwtTokenService(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
        }

        public string GerarToken(Usuario usuario)
        {
            var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Name, usuario.Nome),
                new Claim(ClaimTypes.Role, usuario.Perfil.ToString()),
                new Claim("perfil", usuario.Perfil.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_settings.ExpiracaoHoras),
                signingCredentials: credenciais
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
