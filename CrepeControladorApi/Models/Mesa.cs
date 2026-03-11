namespace CrepeControladorApi.Models
{
    public class Mesa
    {
        public int Id { get; set; }
        public string Numero { get; set; } = null!;
        public int EmpresaId { get; set; }
        public bool Ativa { get; set; } = true;

        public Empresa? Empresa { get; set; }
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
