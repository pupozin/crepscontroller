using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CrepeControladorApi.Services
{
    public class PedidoQueryService
    {
        private readonly AppDbContext _context;

        public PedidoQueryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PedidoResumoDto>> PesquisarPedidosAsync(string termo)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync();
                await connection.EnsureApplicationTimeZoneAsync();
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = @"SELECT *
                    FROM (
                        SELECT * FROM ""sp_PesquisarPedidos""(@Termo)
                    ) AS pedidos(""Id"", ""Codigo"", ""Cliente"", ""TipoPedido"", ""Status"", ""Observacao"", ""DataCriacao"", ""DataConclusao"", ""ValorTotal"")";
                command.CommandType = CommandType.Text;

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@Termo";
                parameter.Value = termo ?? string.Empty;
                command.Parameters.Add(parameter);

                return await MapearPedidosAsync(command);
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task<List<PedidoResumoDto>> ListarPedidosAbertosPorTipoAsync(string tipoPedido)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync();
                await connection.EnsureApplicationTimeZoneAsync();
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = @"SELECT *
                    FROM (
                        SELECT * FROM ""sp_Pedidos_ListarAbertosPorTipoPedido""(@TipoPedido)
                    ) AS pedidos(""Id"", ""Codigo"", ""Cliente"", ""TipoPedido"", ""Status"", ""Observacao"", ""DataCriacao"", ""DataConclusao"", ""ValorTotal"")";
                command.CommandType = CommandType.Text;

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@TipoPedido";
                parameter.Value = tipoPedido;
                command.Parameters.Add(parameter);

                return await MapearPedidosAsync(command);
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task<List<PedidoResumoDto>> ListarPorGrupoStatusAsync(string grupo)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync();
                await connection.EnsureApplicationTimeZoneAsync();
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = @"SELECT *
                    FROM (
                        SELECT * FROM ""sp_Pedidos_ListarPorGrupoStatus""(@Grupo)
                    ) AS pedidos(""Id"", ""Codigo"", ""Cliente"", ""TipoPedido"", ""Status"", ""Observacao"", ""DataCriacao"", ""DataConclusao"", ""ValorTotal"")";
                command.CommandType = CommandType.Text;

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@Grupo";
                parameter.Value = grupo;
                command.Parameters.Add(parameter);

                return await MapearPedidosAsync(command);
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private async Task<List<PedidoResumoDto>> MapearPedidosAsync(DbCommand command)
        {
            var resultados = new List<PedidoResumoDto>();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultados.Add(new PedidoResumoDto
                {
                    Id = GetInt32(reader, "Id", "PedidoId"),
                    Codigo = reader.GetString(reader.GetOrdinal("Codigo")),
                    Cliente = reader.IsDBNull(reader.GetOrdinal("Cliente")) ? null : reader.GetString(reader.GetOrdinal("Cliente")),
                    TipoPedido = reader.GetString(reader.GetOrdinal("TipoPedido")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    Observacao = reader.IsDBNull(reader.GetOrdinal("Observacao")) ? null : reader.GetString(reader.GetOrdinal("Observacao")),
                    DataCriacao = reader.GetDateTime(reader.GetOrdinal("DataCriacao")),
                    DataConclusao = reader.IsDBNull(reader.GetOrdinal("DataConclusao")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DataConclusao")),
                    ValorTotal = reader.GetDecimal(reader.GetOrdinal("ValorTotal"))
                });
            }

            return resultados;
        }

        private static int GetInt32(DbDataReader reader, params string[] columnNames)
        {
            foreach (var column in columnNames)
            {
                try
                {
                    var ordinal = reader.GetOrdinal(column);
                    return reader.GetInt32(ordinal);
                }
                catch (IndexOutOfRangeException)
                {
                    // tenta próxima coluna
                }
            }

            throw new IndexOutOfRangeException($"Nenhuma das colunas [{string.Join(", ", columnNames)}] foi encontrada.");
        }
    }
}
