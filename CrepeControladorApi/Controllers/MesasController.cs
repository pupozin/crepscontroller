using System.ComponentModel.DataAnnotations;
using CrepeControladorApi.Data;
using CrepeControladorApi.Models;
using CrepeControladorApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MesasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserContext _currentUser;

        public MesasController(AppDbContext context, ICurrentUserContext currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery][Range(1, int.MaxValue)] int empresaId,
            [FromQuery] bool apenasLivres = false,
            [FromQuery] int? incluirMesaId = null)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            var fechados = new[] { "Finalizado", "Cancelado" };

            var query = _context.Mesas
                .AsNoTracking()
                .Where(m => m.EmpresaId == empresaId && m.Ativa);

            if (apenasLivres)
            {
                query = query.Where(m =>
                    (incluirMesaId.HasValue && m.Id == incluirMesaId.Value)
                    || !_context.Pedidos.Any(p =>
                        p.MesaId == m.Id
                        && p.EmpresaId == empresaId
                        && !fechados.Contains(p.Status)));
            }

            var mesas = await query
                .OrderBy(m => m.Numero)
                .Select(m => new { m.Id, m.Numero, m.EmpresaId })
                .ToListAsync();

            return Ok(mesas);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Criar([FromBody] Mesa mesa)
        {
            if (!ModelState.IsValid || mesa.EmpresaId <= 0 || string.IsNullOrWhiteSpace(mesa.Numero))
            {
                return ValidationProblem(ModelState);
            }

            if (!_currentUser.EmpresaAutorizada(mesa.EmpresaId))
            {
                return Forbid();
            }

            mesa.Numero = mesa.Numero.Trim();
            mesa.Ativa = true;

            var jaExiste = await _context.Mesas.AnyAsync(m => m.EmpresaId == mesa.EmpresaId && m.Numero == mesa.Numero && m.Ativa);
            if (jaExiste)
            {
                return Conflict("Ja existe uma mesa com esse numero para a empresa.");
            }

            _context.Mesas.Add(mesa);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Listar), new { empresaId = mesa.EmpresaId }, new { mesa.Id, mesa.Numero, mesa.EmpresaId });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] Mesa mesa)
        {
            if (!ModelState.IsValid || id != mesa.Id || mesa.EmpresaId <= 0)
            {
                return ValidationProblem(ModelState);
            }

            if (!_currentUser.EmpresaAutorizada(mesa.EmpresaId))
            {
                return Forbid();
            }

            var existente = await _context.Mesas.FirstOrDefaultAsync(m => m.Id == id && m.EmpresaId == mesa.EmpresaId);
            if (existente == null)
            {
                return NotFound();
            }

            var numero = mesa.Numero.Trim();
            var duplicado = await _context.Mesas.AnyAsync(m => m.EmpresaId == mesa.EmpresaId && m.Id != id && m.Numero == numero && m.Ativa);
            if (duplicado)
            {
                return Conflict("Ja existe uma mesa com esse numero para a empresa.");
            }

            existente.Numero = numero;
            existente.Ativa = true;
            await _context.SaveChangesAsync();
            return Ok(new { existente.Id, existente.Numero, existente.EmpresaId });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Excluir(int id, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            var mesa = await _context.Mesas.FirstOrDefaultAsync(m => m.Id == id && m.EmpresaId == empresaId);
            if (mesa == null)
            {
                return NotFound();
            }

            mesa.Ativa = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
