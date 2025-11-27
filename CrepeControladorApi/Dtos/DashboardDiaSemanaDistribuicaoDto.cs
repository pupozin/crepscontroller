namespace CrepeControladorApi.Dtos
{
    public class DashboardDiaSemanaDistribuicaoDto
    {
        public int DiaSemana { get; set; }
        public string NomeDia { get; set; } = string.Empty;
        public int Hora { get; set; }
        public int QuantidadePedidos { get; set; }
        public decimal Faturamento { get; set; }
    }
}

