using System;
using System.Data;
using System.Data.Common;
using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CrepeControladorApi.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<DashboardHorarioPicoDto>> ObterHorariosPicoPorDiaSemana(byte diaSemana, int? ano)
        {
            return ExecuteListAsync("sp_Dashboard_HorariosPico_DiaSemana", command =>
            {
                AddParameter(command, "@DiaSemana", diaSemana);
                AddParameter(command, "@Ano", ano);
            }, reader => new DashboardHorarioPicoDto
            {
                Hora = reader.GetInt32(reader.GetOrdinal("Hora")),
                QuantidadePedidos = reader.GetInt32(reader.GetOrdinal("QuantidadePedidos")),
                Faturamento = reader.GetDecimal(reader.GetOrdinal("Faturamento"))
            });
        }

        public Task<List<DashboardHorarioPicoDto>> ObterHorariosPicoPorMes(int ano, int mes)
        {
            return ExecuteListAsync("sp_Dashboard_HorariosPico_Mes", command =>
            {
                AddParameter(command, "@Ano", ano);
                AddParameter(command, "@Mes", mes);
            }, reader => new DashboardHorarioPicoDto
            {
                Hora = reader.GetInt32(reader.GetOrdinal("Hora")),
                QuantidadePedidos = reader.GetInt32(reader.GetOrdinal("QuantidadePedidos")),
                Faturamento = reader.GetDecimal(reader.GetOrdinal("Faturamento"))
            });
        }

        public async Task<DashboardResumoPeriodoDto> ObterResumoPeriodo(DateTime dataInicio, DateTime dataFim)
        {
            var resultado = await ExecuteSingleAsync("sp_Dashboard_ResumoPeriodo", command =>
            {
                AddParameter(command, "@DataInicio", dataInicio);
                AddParameter(command, "@DataFim", dataFim);
            }, reader => new DashboardResumoPeriodoDto
            {
                QtdePedidos = reader.GetInt32(reader.GetOrdinal("QtdePedidos")),
                FaturamentoTotal = GetDecimal(reader, "FaturamentoTotal"),
                TicketMedio = GetDecimal(reader, "TicketMedio"),
                QtdeDiasPeriodo = reader.GetInt32(reader.GetOrdinal("QtdeDiasPeriodo")),
                MediaClientesPorDia = GetDecimal(reader, "MediaClientesPorDia")
            });

            return resultado ?? new DashboardResumoPeriodoDto();
        }

        public Task<List<DashboardItemRankingDto>> ObterItensRanking(DateTime dataInicio, DateTime dataFim)
        {
            return ExecuteListAsync("sp_Dashboard_ItensRanking", command =>
            {
                AddParameter(command, "@DataInicio", dataInicio);
                AddParameter(command, "@DataFim", dataFim);
            }, reader => new DashboardItemRankingDto
            {
                ItemId = reader.GetInt32(reader.GetOrdinal("ItemId")),
                Nome = reader.GetString(reader.GetOrdinal("Nome")),
                QuantidadeVendida = reader.GetInt32(reader.GetOrdinal("QuantidadeVendida")),
                Faturamento = reader.GetDecimal(reader.GetOrdinal("Faturamento"))
            });
        }

        public Task<List<DashboardTipoPedidoDto>> ObterTipoPedido(DateTime dataInicio, DateTime dataFim)
        {
            return ExecuteListAsync("sp_Dashboard_TipoPedido", command =>
            {
                AddParameter(command, "@DataInicio", dataInicio);
                AddParameter(command, "@DataFim", dataFim);
            }, reader => new DashboardTipoPedidoDto
            {
                TipoPedido = reader.GetString(reader.GetOrdinal("TipoPedido")),
                QtdePedidos = reader.GetInt32(reader.GetOrdinal("QtdePedidos")),
                Faturamento = reader.GetDecimal(reader.GetOrdinal("Faturamento"))
            });
        }

        private async Task<List<T>> ExecuteListAsync<T>(string storedProcedure, Action<DbCommand> configure, Func<DbDataReader, T> map)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync();
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = storedProcedure;
                command.CommandType = CommandType.StoredProcedure;
                configure?.Invoke(command);

                var result = new List<T>();

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(map(reader));
                }

                return result;
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private async Task<T> ExecuteSingleAsync<T>(string storedProcedure, Action<DbCommand> configure, Func<DbDataReader, T> map)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync();
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = storedProcedure;
                command.CommandType = CommandType.StoredProcedure;
                configure?.Invoke(command);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return map(reader);
                }

                return default!;
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private static void AddParameter(DbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private static decimal GetDecimal(DbDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
        }
    }
}
