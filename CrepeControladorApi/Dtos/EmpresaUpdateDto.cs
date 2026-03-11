using System.ComponentModel.DataAnnotations;

namespace CrepeControladorApi.Dtos
{
    public class EmpresaUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(18)]
        public string Cnpj { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Nome { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string RazaoSocial { get; set; } = null!;
    }
}
