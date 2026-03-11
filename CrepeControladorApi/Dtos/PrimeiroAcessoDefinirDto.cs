using System.ComponentModel.DataAnnotations;

namespace CrepeControladorApi.Dtos
{
    public class PrimeiroAcessoDefinirDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(4)]
        public string Senha { get; set; } = null!;
    }
}
