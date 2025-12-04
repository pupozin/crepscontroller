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
        private const string StatusFinalizado = "Finalizado";

        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<DashboardHorarioPicoDto>> ObterHorariosPicoPorPeriodo(DateTime dataInicio, DateTime dataFim)
        {
            return ExecuteListAsync("SELECT * FROM \"sp_Dashboard_HorariosPico_Periodo\"(@DataInicio::DATE, @DataFim::DATE)", command =>
            {
                AddDateParameter(command, "@DataInicio", dataInicio);
                AddDateParameter(command, "@DataFim", dataFim);
            }, reader => new DashboardHorarioPicoDto
            {
                Hora = GetInt32(reader, "Hora"),
                QuantidadePedidos = GetInt32(reader, "QuantidadePedidos"),
                Faturamento = reader.GetDecimal(reader.GetOrdinal("Faturamento"))
            });
        }

        public Task<List<DashboardDiaSemanaPicoDto>> ObterHorariosPicoDiaSemanaResumo(DateTime dataInicio, DateTime dataFim)
        {
            return ExecuteListAsync("SELECT * FROM \"sp_Dashboard_HorariosPico_DiaSemanaResumo\"(@DataInicio::DATE, @DataFim::DATE)", command =>
            {
                AddDateParameter(command, "@DataInicio", dataInicio);
                AddDateParameter(command, "@DataFim", dataFim);
            }, reader => new DashboardDiaSemanaPicoDto
            {
                DiaSemana = GetInt32(reader, "DiaSemana"),
                NomeDia = reader.GetString(reader.GetOrdinal("NomeDia")),
                Hora = GetInt32(reader, "Hora"),
                QuantidadePedidos = GetInt32(reader, "QuantidadePedidos"),
                Faturamento = reader.GetDecimal(reader.GetOrdinal("Faturamento"))
            });
        }

        public Task<List<DashboardDiaSemanaDistribuicaoDto>> ObterDistribuicaoDiaSemanaPorHora(DateTime dataInicio, DateTime dataFim)
        {
            return ExecuteListAsync("SELECT * FROM \"sp_Dashboard_HorariosPico_DiaSemanaDistribuicao\"(@DataInicio::DATE, @DataFim::DATE)", command =>
            {
                AddDateParameter(command, "@DataInicio", dataInicio);
                AddDateParameter(command, "@DataFim", dataFim);
            }, reader => new DashboardDiaSemanaDistribuicaoDto
            {
                DiaSemana = GetInt32(reader, "DiaSemana"),
                NomeDia = reader.GetString(reader.GetOrdinal("NomeDia")),
                Hora = GetInt32(reader, "Hora"),
                QuantidadePedidos = GetInt32(reader, "QuantidadePedidos"),
                Faturamento = reader.GetDecimal(reader.GetOrdinal("Faturamento"))
            });
        }

        public async Task<DashboardResumoPeriodoDto> ObterResumoPeriodo(DateTime? dataInicio, DateTime? dataFim)
        {
            var resultado = await ExecuteSingleAsync("SELECT * FROM \"sp_Dashboard_ResumoPeriodo\"(@DataInicio::DATE, @DataFim::DATE)", command =>
            {
                AddDateParameter(command, "@DataInicio", dataInicio);
                AddDateParameter(command, "@DataFim", dataFim);
            }, reader => new DashboardResumoPeriodoDto
            {
                QtdePedidos = GetInt32(reader, "QtdePedidos"),
                FaturamentoTotal = GetDecimal(reader, "FaturamentoTotal"),
                TicketMedio = GetDecimal(reader, "TicketMedio"),
                QtdeDiasPeriodo = GetInt32(reader, "QtdeDiasPeriodo"),
                MediaClientesPorDia = GetDecimal(reader, "MediaClientesPorDia")
            });

            return resultado ?? new DashboardResumoPeriodoDto();
        }

        public Task<List<DashboardItemRankingDto>> ObterItensRanking(DateTime? dataInicio, DateTime? dataFim)
        {
            return ExecuteListAsync("SELECT * FROM \"sp_Dashboard_ItensRanking\"(@DataInicio::DATE, @DataFim::DATE)", command =>
            {
                AddDateParameter(command, "@DataInicio", dataInicio);
                AddDateParameter(command, "@DataFim", dataFim);
            }, reader => new DashboardItemRankingDto
            {
                ItemId = GetInt32(reader, "ItemId"),
                Nome = reader.GetString(reader.GetOrdinal("Nome")),
                QuantidadeVendida = GetInt32(reader, "QuantidadeVendida"),
                Faturamento = reader.GetDecimal(reader.GetOrdinal("Faturamento"))
            });
        }

        public Task<List<DashboardTipoPedidoDto>> ObterTipoPedido(DateTime? dataInicio, DateTime? dataFim)
        {
            return ExecuteListAsync("SELECT * FROM \"sp_Dashboard_TipoPedido\"(@DataInicio::DATE, @DataFim::DATE)", command =>
            {
                AddDateParameter(command, "@DataInicio", dataInicio);
                AddDateParameter(command, "@DataFim", dataFim);
            }, reader => new DashboardTipoPedidoDto
            {
                TipoPedido = reader.GetString(reader.GetOrdinal("TipoPedido")),
                QtdePedidos = GetInt32(reader, "QtdePedidos"),
                Faturamento = reader.GetDecimal(reader.GetOrdinal("Faturamento"))
            });
        }

        public async Task<DashboardPeriodoTotalDto?> ObterPeriodoTotal()
        {
            var finalizados = _context.Pedidos.Where(p => p.Status == StatusFinalizado);
            if (!await finalizados.AnyAsync())
            {
                return null;
            }

            var dataInicial = await finalizados.MinAsync(p => p.DataCriacao);
            var dataFinal = await finalizados.MaxAsync(p => p.DataCriacao);

            return new DashboardPeriodoTotalDto
            {
                DataInicio = dataInicial.Date,
                DataFim = dataFinal.Date
            };
        }

        private async Task<List<T>> ExecuteListAsync<T>(string storedProcedure, Action<DbCommand> configure, Func<DbDataReader, T> map)
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
                command.CommandText = storedProcedure;
                command.CommandType = CommandType.Text;
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
                await connection.EnsureApplicationTimeZoneAsync();
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = storedProcedure;
                command.CommandType = CommandType.Text;
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

        protected static void AddParameter(DbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private static void AddDateParameter(DbCommand command, string name, DateTime? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            if (value.HasValue)
            {
                parameter.Value = DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Unspecified);
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
            parameter.DbType = DbType.Date;
            command.Parameters.Add(parameter);
        }

        private static decimal GetDecimal(DbDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
        }

        private static int GetInt32(DbDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }

    }
}
