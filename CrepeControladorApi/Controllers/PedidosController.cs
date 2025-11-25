using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using CrepeControladorApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        [HttpGet]
        public async Task<IActionResult> ObterPedidos()
        {
            var pedidos = await _context.Pedidos
                .FromSqlRaw("EXEC sp_Pedido_Obter")
                .AsNoTracking()
                .ToListAsync();

            return Ok(pedidos);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPedido(int id)
        {
            var pedidos = await _context.Pedidos
                .FromSqlInterpolated($"EXEC sp_Pedido_Obter @PedidoId = {id}")
                .AsNoTracking()
                .ToListAsync();

            var pedido = pedidos.FirstOrDefault();

            if (pedido == null)
            {
                return NotFound();
            }

            var itens = await ObterItensPedidoPorProcedure(id);

            return Ok(new
            {
                pedido.Id,
                pedido.Codigo,
                pedido.Cliente,
                pedido.TipoPedido,
                pedido.Status,
                pedido.Observacao,
                pedido.DataCriacao,
                pedido.DataConclusao,
                pedido.ValorTotal,
                Itens = itens
            });
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

        [HttpPut("{id:int}")]
        public async Task<IActionResult> AtualizarPedido(int id, [FromBody] PedidoUpdateDto pedidoDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            if (!StatusPermiteAlteracao(pedido.Status))
            {
                return BadRequest("Pedido so pode ser atualizado quando estiver Preparando ou Pronto.");
            }

            if (pedidoDto.Itens == null || pedidoDto.Itens.Count == 0)
            {
                ModelState.AddModelError(nameof(pedidoDto.Itens), "Informe ao menos um item para o pedido.");
                return ValidationProblem(ModelState);
            }

            pedido.Codigo = pedidoDto.Codigo;
            pedido.Cliente = pedidoDto.Cliente;
            pedido.TipoPedido = pedidoDto.TipoPedido;
            pedido.Status = pedidoDto.Status;
            pedido.Observacao = pedidoDto.Observacao;

            decimal valorTotal = 0m;
            var novosItens = new List<ItensPedido>();

            foreach (var itemDto in pedidoDto.Itens)
            {
                var item = await _context.Itens.FindAsync(itemDto.ItemId);
                if (item == null)
                {
                    ModelState.AddModelError(nameof(pedidoDto.Itens), $"Item com Id {itemDto.ItemId} nao foi encontrado.");
                    return ValidationProblem(ModelState);
                }

                var totalItem = item.Preco * itemDto.Quantidade;

                novosItens.Add(new ItensPedido
                {
                    ItemId = item.Id,
                    Quantidade = itemDto.Quantidade,
                    PrecoUnitario = item.Preco,
                    TotalItem = totalItem
                });

                valorTotal += totalItem;
            }

            _context.ItensPedidos.RemoveRange(pedido.Itens);
            pedido.Itens.Clear();

            foreach (var novoItem in novosItens)
            {
                pedido.Itens.Add(novoItem);
            }

            pedido.ValorTotal = valorTotal;

            await _context.SaveChangesAsync();

            return Ok(new
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

        private static bool StatusPermiteAlteracao(string status)
        {
            return string.Equals(status, "Preparando", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Pronto", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<List<PedidoItemDetalheDto>> ObterItensPedidoPorProcedure(int pedidoId)
        {
            var itens = new List<PedidoItemDetalheDto>();
            var connection = _context.Database.GetDbConnection();
            var fecharConexao = connection.State != ConnectionState.Open;

            if (fecharConexao)
            {
                await connection.OpenAsync();
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "sp_ItensPedido_ObterPorPedido";
                command.CommandType = CommandType.StoredProcedure;

                var parametro = command.CreateParameter();
                parametro.ParameterName = "@PedidoId";
                parametro.Value = pedidoId;
                command.Parameters.Add(parametro);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    itens.Add(new PedidoItemDetalheDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        PedidoId = reader.GetInt32(reader.GetOrdinal("PedidoId")),
                        ItemId = reader.GetInt32(reader.GetOrdinal("ItemId")),
                        NomeItem = reader.GetString(reader.GetOrdinal("NomeItem")),
                        PrecoItem = reader.GetDecimal(reader.GetOrdinal("PrecoItem")),
                        ItemAtivo = reader.GetBoolean(reader.GetOrdinal("ItemAtivo")),
                        Quantidade = reader.GetInt32(reader.GetOrdinal("Quantidade")),
                        PrecoUnitario = reader.GetDecimal(reader.GetOrdinal("PrecoUnitario")),
                        TotalItem = reader.GetDecimal(reader.GetOrdinal("TotalItem"))
                    });
                }
            }
            finally
            {
                if (fecharConexao)
                {
                    await connection.CloseAsync();
                }
            }

            return itens;
        }
    }
}
