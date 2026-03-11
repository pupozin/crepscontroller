using System.ComponentModel.DataAnnotations;

namespace CrepeControladorApi.Dtos
{
    public class PrimeiroAcessoDefinirDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        [MaxLength(128)]
        [RegularExpression("^(?=.*[a-zA-Z])(?=.*\\d).+$", ErrorMessage = "A senha deve conter letras e nÇ§meros.")]
        public string Senha { get; set; } = null!;
    }
}
