using System.ComponentModel.DataAnnotations;

namespace CrepeControladorApi.Dtos
{
    public class ItemCreateDto
    {
        [Required]
        [StringLength(150)]
        public string Nome { get; set; } = null!;

        [Range(0.01, 999999)]
        public decimal Preco { get; set; }

        public bool Ativo { get; set; } = true;
    }
}
