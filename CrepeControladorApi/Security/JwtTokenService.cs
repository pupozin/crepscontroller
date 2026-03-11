using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CrepeControladorApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CrepeControladorApi.Security
{
    public class JwtTokenService
    {
        private readonly JwtOptions _options;
        private readonly byte[] _keyBytes;

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                throw new InvalidOperationException("JWT secret key is not configured. Set Jwt:SecretKey via environment variable.");
            }

            _keyBytes = Encoding.UTF8.GetBytes(_options.SecretKey);
        }

        public (string Token, DateTime ExpiresAtUtc) GerarToken(Usuario usuario)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(Math.Max(1, _options.AccessTokenMinutes));

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(ClaimTypes.Email, usuario.Email),
                new(ClaimTypes.Name, usuario.Nome),
                new("empresaId", usuario.EmpresaId.ToString()),
                new("perfilId", usuario.PerfilId.ToString())
            };

            if (!string.IsNullOrWhiteSpace(usuario.Perfil?.Nome))
            {
                claims.Add(new(ClaimTypes.Role, usuario.Perfil.Nome));
                claims.Add(new("perfilNome", usuario.Perfil.Nome));
            }

            var credentials = new SigningCredentials(new SymmetricSecurityKey(_keyBytes), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new JwtSecurityToken(
                _options.Issuer,
                _options.Audience,
                claims,
                expires: expiresAt,
                signingCredentials: credentials);

            var tokenHandler = new JwtSecurityTokenHandler();
            return (tokenHandler.WriteToken(tokenDescriptor), expiresAt);
        }
    }
}
