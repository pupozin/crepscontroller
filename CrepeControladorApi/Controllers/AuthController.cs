using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using CrepeControladorApi.Models;
using CrepeControladorApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AuthController> _logger;
        private const int MaxLoginAttempts = 5;
        private static readonly TimeSpan LoginWindow = TimeSpan.FromMinutes(10);

        public AuthController(
            AppDbContext context,
            JwtTokenService jwtTokenService,
            IPasswordHasher<Usuario> passwordHasher,
            IMemoryCache cache,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _passwordHasher = passwordHasher;
            _cache = cache;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (IsLoginRateLimited(loginDto.Email, out var retryAfter))
            {
                Response.Headers["Retry-After"] = retryAfter.TotalSeconds.ToString("F0");
                return StatusCode(StatusCodes.Status429TooManyRequests, "Muitas tentativas. Aguarde antes de tentar novamente.");
            }

            var normalizedEmail = loginDto.Email.Trim().ToLowerInvariant();

            var usuario = await _context.Usuarios
                .Include(u => u.Empresa)
                .Include(u => u.Perfil)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (usuario == null || string.IsNullOrEmpty(usuario.SenhaHash))
            {
                RegisterLoginFailure(normalizedEmail);
                _logger.LogWarning("Tentativa de login falhou para {Email}", normalizedEmail);
                return Unauthorized("Credenciais invalidas.");
            }

            var verification = _passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, loginDto.Senha);
            if (verification == PasswordVerificationResult.Failed)
            {
                RegisterLoginFailure(normalizedEmail);
                _logger.LogWarning("Tentativa de login com senha invalida para {Email}", normalizedEmail);
                return Unauthorized("Credenciais invalidas.");
            }

            ResetLoginAttempts(normalizedEmail);
            var resposta = MapearResposta(usuario);
            _logger.LogInformation("Usuario {Email} autenticado com sucesso", normalizedEmail);
            return Ok(resposta);
        }

        [HttpPost("primeiro-acesso/verificar")]
        public async Task<IActionResult> VerificarPrimeiroAcesso([FromBody] PrimeiroAcessoEmailDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (usuario == null)
            {
                return NotFound("Usuario inexistente.");
            }

            if (!string.IsNullOrWhiteSpace(usuario.SenhaHash))
            {
                return BadRequest("Usuario ja possui senha.");
            }

            return Ok(new { PodeDefinir = true });
        }

        [HttpPost("primeiro-acesso/definir")]
        public async Task<IActionResult> DefinirPrimeiroAcesso([FromBody] PrimeiroAcessoDefinirDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            var usuario = await _context.Usuarios
                .Include(u => u.Empresa)
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (usuario == null)
            {
                return NotFound("Usuario inexistente.");
            }

            if (!string.IsNullOrWhiteSpace(usuario.SenhaHash))
            {
                return BadRequest("Usuario ja possui senha.");
            }

            usuario.SenhaHash = _passwordHasher.HashPassword(usuario, dto.Senha);
            await _context.SaveChangesAsync();

            var resposta = MapearResposta(usuario);

            return Ok(resposta);
        }

        private LoginResponseDto MapearResposta(Usuario usuario)
        {
            var (token, expiresAt) = _jwtTokenService.GerarToken(usuario);

            return new LoginResponseDto
            {
                Id = usuario.Id,
                Email = usuario.Email,
                Nome = usuario.Nome,
                EmpresaId = usuario.EmpresaId,
                EmpresaNome = usuario.Empresa?.Nome ?? string.Empty,
                PerfilId = usuario.PerfilId,
                PerfilNome = usuario.Perfil?.Nome ?? string.Empty,
                Token = token,
                ExpiresAtUtc = expiresAt
            };
        }

        private bool IsLoginRateLimited(string email, out TimeSpan retryAfter)
        {
            var key = BuildLoginCacheKey(email);
            if (_cache.TryGetValue<LoginAttempt>(key, out var attempt) && attempt.ExpiresAtUtc > DateTime.UtcNow)
            {
                retryAfter = attempt.ExpiresAtUtc - DateTime.UtcNow;
                return attempt.Count >= MaxLoginAttempts;
            }

            retryAfter = TimeSpan.Zero;
            return false;
        }

        private void RegisterLoginFailure(string email)
        {
            var key = BuildLoginCacheKey(email);
            var attempt = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = LoginWindow;
                return new LoginAttempt { Count = 0, ExpiresAtUtc = DateTime.UtcNow.Add(LoginWindow) };
            })!;

            attempt.Count++;
            attempt.ExpiresAtUtc = DateTime.UtcNow.Add(LoginWindow);
            _cache.Set(key, attempt, attempt.ExpiresAtUtc);
        }

        private void ResetLoginAttempts(string email)
        {
            _cache.Remove(BuildLoginCacheKey(email));
        }

        private string BuildLoginCacheKey(string email)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"login:{email.ToLowerInvariant()}:{ip}";
        }

        private sealed class LoginAttempt
        {
            public int Count { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
        }
    }
}
