using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CrepeControladorApi.Services
{
    public class PedidoQueryService
    {
        private readonly AppDbContext _context;
        private static readonly string[] StatusFechados = { "Finalizado", "Cancelado" };

        public PedidoQueryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PedidoResumoDto>> PesquisarPedidosAsync(string termo, int empresaId)
        {
            var query = _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Mesa)
                .Where(p => p.EmpresaId == empresaId);

            if (!string.IsNullOrWhiteSpace(termo))
            {
                termo = termo.Trim().ToLowerInvariant();
                query = query.Where(p =>
                    (p.Codigo != null && p.Codigo.ToLower().Contains(termo)) ||
                    (p.Cliente != null && p.Cliente.ToLower().Contains(termo)));
            }

            return await MapearPedidosAsync(query.OrderByDescending(p => p.DataCriacao));
        }

        public Task<List<PedidoResumoDto>> ListarPedidosAbertosPorTipoAsync(string tipoPedido, int empresaId)
        {
            var query = _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Mesa)
                .Where(p => p.EmpresaId == empresaId &&
                            p.TipoPedido == tipoPedido &&
                            !StatusFechados.Contains(p.Status));

            return MapearPedidosAsync(query.OrderByDescending(p => p.DataCriacao));
        }

        public Task<List<PedidoResumoDto>> ListarPorGrupoStatusAsync(string grupo, int empresaId)
        {
            var query = _context.Pedidos.AsNoTracking().Where(p => p.EmpresaId == empresaId);
            query = query.Include(p => p.Mesa);

            if (string.Equals(grupo, "ABERTOS", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => !StatusFechados.Contains(p.Status));
            }
            else
            {
                query = query.Where(p => StatusFechados.Contains(p.Status));
            }

            return MapearPedidosAsync(query.OrderByDescending(p => p.DataCriacao));
        }

        private static async Task<List<PedidoResumoDto>> MapearPedidosAsync(IQueryable<Models.Pedido> query)
        {
            var pedidos = await query.ToListAsync();

            return pedidos.ConvertAll(p => new PedidoResumoDto
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Cliente = p.Cliente,
                TipoPedido = p.TipoPedido,
                Status = p.Status,
                Observacao = p.Observacao,
                DataCriacao = p.DataCriacao,
                DataConclusao = p.DataConclusao,
                ValorTotal = p.ValorTotal,
                EmpresaId = p.EmpresaId,
                Endereco = p.Endereco,
                MesaId = p.MesaId,
                MesaNumero = p.Mesa?.Numero
            });
        }
    }
}
