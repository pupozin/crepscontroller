namespace CrepeControladorApi.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public decimal Preco { get; set; }
        public bool Ativo { get; set; } = true;
        public int EmpresaId { get; set; }

        // Navegacao
        public Empresa? Empresa { get; set; }
        public ICollection<ItensPedido>? ItensPedido { get; set; }
    }
}
