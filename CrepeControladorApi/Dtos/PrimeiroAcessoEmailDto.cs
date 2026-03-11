using System.ComponentModel.DataAnnotations;

namespace CrepeControladorApi.Dtos
{
    public class PrimeiroAcessoEmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}
