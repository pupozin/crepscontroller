using System.Linq;
using System.ComponentModel.DataAnnotations;
using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using CrepeControladorApi.Models;
using CrepeControladorApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserContext _currentUser;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(AppDbContext context, ICurrentUserContext currentUser, ILogger<UsuariosController> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ListarPorEmpresa([FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            var lista = new List<object>();
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync();
                await connection.EnsureApplicationTimeZoneAsync();
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM \"sp_Admin_Usuarios_ListarPorEmpresa\"(@EmpresaId)";
                var p = command.CreateParameter();
                p.ParameterName = "@EmpresaId";
                p.Value = empresaId;
                command.Parameters.Add(p);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    lista.Add(new
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Nome = reader.GetString(reader.GetOrdinal("Nome")),
                        PerfilId = reader.GetInt32(reader.GetOrdinal("PerfilId")),
                        PerfilNome = reader.IsDBNull(reader.GetOrdinal("PerfilNome")) ? string.Empty : reader.GetString(reader.GetOrdinal("PerfilNome"))
                    });
                }
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }

            return Ok(lista);
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] UsuarioCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (!_currentUser.EmpresaAutorizada(dto.EmpresaId))
            {
                return Forbid();
            }

            var perfilExiste = await _context.Perfis.AnyAsync(p => p.Id == dto.PerfilId);
            var empresaExiste = await _context.Empresas.AnyAsync(e => e.Id == dto.EmpresaId);
            if (!perfilExiste || !empresaExiste)
            {
                return BadRequest("Perfil ou empresa invalidos.");
            }

            var usuario = new Usuario
            {
                Email = dto.Email,
                Nome = dto.Nome,
                PerfilId = dto.PerfilId,
                EmpresaId = dto.EmpresaId,
                SenhaHash = null
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Usuario {Email} criado para empresa {EmpresaId} por {Admin}", usuario.Email, usuario.EmpresaId, _currentUser.UsuarioId);
            return CreatedAtAction(nameof(ListarPorEmpresa), new { empresaId = dto.EmpresaId }, new { usuario.Id, usuario.Email, usuario.Nome, usuario.PerfilId, usuario.EmpresaId });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] UsuarioUpdateDto dto)
        {
            if (!ModelState.IsValid || id != dto.Id)
            {
                return ValidationProblem(ModelState);
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null)
            {
                return NotFound();
            }

            if (!_currentUser.EmpresaAutorizada(usuario.EmpresaId))
            {
                return Forbid();
            }

            usuario.Email = dto.Email;
            usuario.Nome = dto.Nome;
            await _context.SaveChangesAsync();
            return Ok(new { usuario.Id, usuario.Email, usuario.Nome, usuario.EmpresaId, usuario.PerfilId });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Excluir(int id)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null)
            {
                return NotFound();
            }

            if (!_currentUser.EmpresaAutorizada(usuario.EmpresaId))
            {
                return Forbid();
            }

            var perfilNome = await _context.Perfis.Where(p => p.Id == usuario.PerfilId).Select(p => p.Nome).FirstOrDefaultAsync();
            var isAdmin = string.Equals(perfilNome, "Admin", StringComparison.OrdinalIgnoreCase);
            if (isAdmin)
            {
                var totalAdmins = await _context.Usuarios
                    .CountAsync(u => u.EmpresaId == usuario.EmpresaId && u.PerfilId == usuario.PerfilId);
                if (totalAdmins <= 1)
                {
                    return BadRequest("Nao e possivel excluir o unico usuario administrador da empresa.");
                }
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Usuario {UsuarioId} removido da empresa {EmpresaId} por {Admin}", usuario.Id, usuario.EmpresaId, _currentUser.UsuarioId);
            return NoContent();
        }
    }
}
