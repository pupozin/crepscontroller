using System.ComponentModel.DataAnnotations;

namespace CrepeControladorApi.Dtos
{
    public class PedidoUpdateDto
    {
        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = null!;

        [StringLength(100)]
        public string? Cliente { get; set; }

        [Required]
        [StringLength(20)]
        public string TipoPedido { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = null!;

        [StringLength(250)]
        public string? Observacao { get; set; }

        [MinLength(1, ErrorMessage = "O pedido precisa de ao menos um item.")]
        public List<PedidoItemCreateDto> Itens { get; set; } = new();
    }
}
