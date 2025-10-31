using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Lantean.QBTMud.Models
{
    public class LoginForm
    {
        [Required]
        [NotNull]
        public string? Username { get; set; }

        [Required]
        [NotNull]
        public string? Password { get; set; }
    }
}