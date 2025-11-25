using System.Linq;
using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using CrepeControladorApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PedidosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CriarPedido([FromBody] PedidoCreateDto pedidoDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (pedidoDto.Itens == null || pedidoDto.Itens.Count == 0)
            {
                ModelState.AddModelError(nameof(pedidoDto.Itens), "Informe ao menos um item para o pedido.");
                return ValidationProblem(ModelState);
            }

            var pedido = new Pedido
            {
                Codigo = pedidoDto.Codigo,
                Cliente = pedidoDto.Cliente,
                TipoPedido = pedidoDto.TipoPedido,
                Observacao = pedidoDto.Observacao
            };

            decimal valorTotal = 0m;

            foreach (var itemDto in pedidoDto.Itens)
            {
                var item = await _context.Itens.FindAsync(itemDto.ItemId);
                if (item == null)
                {
                    ModelState.AddModelError(nameof(pedidoDto.Itens), $"Item com Id {itemDto.ItemId} nao foi encontrado.");
                    return ValidationProblem(ModelState);
                }

                var totalItem = item.Preco * itemDto.Quantidade;

                pedido.Itens.Add(new ItensPedido
                {
                    ItemId = item.Id,
                    Quantidade = itemDto.Quantidade,
                    PrecoUnitario = item.Preco,
                    TotalItem = totalItem
                });

                valorTotal += totalItem;
            }

            pedido.ValorTotal = valorTotal;

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            return Created($"api/pedidos/{pedido.Id}", new
            {
                pedido.Id,
                pedido.Codigo,
                pedido.Status,
                pedido.ValorTotal,
                Itens = pedido.Itens.Select(i => new
                {
                    i.ItemId,
                    i.Quantidade,
                    i.TotalItem
                })
            });
        }
    }
}
