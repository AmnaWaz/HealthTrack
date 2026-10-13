using System;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class RegisterViewModel
    {
        [Required, StringLength(50), Display(Name = "First name")]
        public string FirstName { get; set; }

        [Required, StringLength(50), Display(Name = "Last name")]
        public string LastName { get; set; }

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required (you can log in with it)."), Phone, StringLength(25)]
        public string Phone { get; set; }

        [Required, MinLength(6), DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; }          // "Client" or "Trainer"

        // Client only
        [DataType(DataType.Date), Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public string Goal { get; set; }

        // Trainer only
        public string Specialisation { get; set; }
        public string Bio { get; set; }
    }

    public class LoginViewModel
    {
        public string Method { get; set; }        // "Email" or "Phone"

        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string Phone { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
