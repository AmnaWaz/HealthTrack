using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class AdminLoginViewModel
    {
        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class AdminRegisterViewModel
    {
        [Required, StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; }

        [Required, StringLength(100, MinimumLength = 12)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{12,}$", ErrorMessage = "Use 12+ characters with upper and lower case letters and a number.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, Compare("Password")]
        [Display(Name = "Confirm password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}
