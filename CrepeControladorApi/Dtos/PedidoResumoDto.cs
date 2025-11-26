using System;

namespace CrepeControladorApi.Dtos
{
    public class PedidoResumoDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string? Cliente { get; set; }
        public string TipoPedido { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Observacao { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataConclusao { get; set; }
        public decimal ValorTotal { get; set; }
    }
}
