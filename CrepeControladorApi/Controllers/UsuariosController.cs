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
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ListarPorEmpresa([FromQuery] int empresaId)
        {
            var usuarios = await _context.Usuarios
                .FromSqlRaw("SELECT * FROM \"sp_Admin_Usuarios_ListarPorEmpresa\"({0})", empresaId)
                .AsNoTracking()
                .ToListAsync();

            var perfis = await _context.Perfis.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nome);

            return Ok(usuarios.Select(u => new
            {
                u.Id,
                u.Email,
                u.Nome,
                u.EmpresaId,
                u.PerfilId,
                PerfilNome = perfis.TryGetValue(u.PerfilId, out var nomePerfil) ? nomePerfil : string.Empty
            }));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] UsuarioUpdateDto dto)
        {
            if (!ModelState.IsValid || id != dto.Id)
            {
                return ValidationProblem(ModelState);
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }
            usuario.Email = dto.Email;
            usuario.Nome = dto.Nome;
            await _context.SaveChangesAsync();
            return Ok(new { usuario.Id, usuario.Email, usuario.Nome, usuario.EmpresaId, usuario.PerfilId });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Excluir(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            // Impede remover o único admin da empresa
            var perfilNome = await _context.Perfis.Where(p => p.Id == usuario.PerfilId).Select(p => p.Nome).FirstOrDefaultAsync();
            var isAdmin = string.Equals(perfilNome, "Admin", StringComparison.OrdinalIgnoreCase);
            if (isAdmin)
            {
                var totalAdmins = await _context.Usuarios
                    .CountAsync(u => u.EmpresaId == usuario.EmpresaId && u.PerfilId == usuario.PerfilId);
                if (totalAdmins <= 1)
                {
                    return BadRequest("Não é possível excluir o único usuário administrador da empresa.");
                }
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
