using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace SyntaxErrorIDE.Pages
{
    public class RegisterModel : PageModel
    {
        [BindProperty]
        [Required]
        public string Name { get; set; } = null!;

        [BindProperty]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [BindProperty]
        [Required]
        public string Password { get; set; } = null!;

        [BindProperty]
        [Required]
        [Display(Name = "Repeat Password")]
        public string PasswordRepeat { get; set; } = null!;

        public string Message { get; set; } = null!;

        public void OnGet()
        {
        }
    }
}