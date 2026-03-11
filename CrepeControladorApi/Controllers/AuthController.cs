using System.Linq;
using System.Threading.Tasks;
using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Empresa)
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower());

            if (usuario == null || string.IsNullOrEmpty(usuario.Senha) || usuario.Senha != loginDto.Senha)
            {
                return Unauthorized("Credenciais invalidas.");
            }

            var resposta = new LoginResponseDto
            {
                Id = usuario.Id,
                Email = usuario.Email,
                Nome = usuario.Nome,
                EmpresaId = usuario.EmpresaId,
                EmpresaNome = usuario.Empresa?.Nome ?? string.Empty,
                PerfilId = usuario.PerfilId,
                PerfilNome = usuario.Perfil?.Nome ?? string.Empty
            };

            return Ok(resposta);
        }

        [HttpPost("primeiro-acesso/verificar")]
        public async Task<IActionResult> VerificarPrimeiroAcesso([FromBody] PrimeiroAcessoEmailDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (usuario == null)
            {
                return NotFound("Usuario inexistente.");
            }

            if (!string.IsNullOrWhiteSpace(usuario.Senha))
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

            var usuario = await _context.Usuarios
                .Include(u => u.Empresa)
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (usuario == null)
            {
                return NotFound("Usuario inexistente.");
            }

            if (!string.IsNullOrWhiteSpace(usuario.Senha))
            {
                return BadRequest("Usuario ja possui senha.");
            }

            usuario.Senha = dto.Senha;
            await _context.SaveChangesAsync();

            var resposta = new LoginResponseDto
            {
                Id = usuario.Id,
                Email = usuario.Email,
                Nome = usuario.Nome,
                EmpresaId = usuario.EmpresaId,
                EmpresaNome = usuario.Empresa?.Nome ?? string.Empty,
                PerfilId = usuario.PerfilId,
                PerfilNome = usuario.Perfil?.Nome ?? string.Empty
            };

            return Ok(resposta);
        }
    }
}
