using System.ComponentModel.DataAnnotations;

namespace CrepeControladorApi.Dtos
{
    public class PedidoItemCreateDto
    {
        [Required]
        public int ItemId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantidade { get; set; }
    }
}
