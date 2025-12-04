using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace CrepeControladorApi.Data
{
    public static class DbConnectionExtensions
    {
        private const string DefaultTimeZone = "America/Sao_Paulo";

        /// <summary>
        /// Applies the application timezone to the current database session.
        /// Must be called only when the connection is already open.
        /// </summary>
        public static async Task EnsureApplicationTimeZoneAsync(this DbConnection connection, string? timeZone = null)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (connection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("Abra a conexao antes de configurar o fuso horario.");
            }

            var zone = string.IsNullOrWhiteSpace(timeZone) ? DefaultTimeZone : timeZone.Trim();
            var sanitizedZone = zone.Replace("'", "''");

            await using var command = connection.CreateCommand();
            command.CommandText = $"SET TIME ZONE '{sanitizedZone}'";
            await command.ExecuteNonQueryAsync();
        }
    }
}
