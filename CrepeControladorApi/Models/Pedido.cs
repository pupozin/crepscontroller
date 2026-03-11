namespace CrepeControladorApi.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string? Cliente { get; set; }
        public string TipoPedido { get; set; } = null!;
        public string Status { get; set; } = "Preparando";
        public string? Observacao { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataConclusao { get; set; }
        public decimal ValorTotal { get; set; }
        public string? Endereco { get; set; }
        public int? MesaId { get; set; }
        public int EmpresaId { get; set; }

        // Navegacao
        public Empresa? Empresa { get; set; }
        public Mesa? Mesa { get; set; }
        public ICollection<ItensPedido> Itens { get; set; } = new List<ItensPedido>();
    }
}
