using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using CrepeControladorApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItemsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CriarItem([FromBody] ItemCreateDto itemDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var item = new Item
            {
                Nome = itemDto.Nome,
                Preco = itemDto.Preco,
                Ativo = itemDto.Ativo
            };

            _context.Itens.Add(item);
            await _context.SaveChangesAsync();

            return Created($"api/items/{item.Id}", new
            {
                item.Id,
                item.Nome,
                item.Preco,
                item.Ativo
            });
        }
    }
}
