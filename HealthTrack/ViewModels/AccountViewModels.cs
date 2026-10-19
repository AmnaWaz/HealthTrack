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

        // Same rule the strength meter on Register.cshtml shows: 8+ characters, upper and lower case, a number.
        // (Login does not re-check this, so accounts created under the old 6-character rule can still sign in.)
        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Use 8 to 100 characters.")]
        [RegularExpression(PasswordRule.Pattern, ErrorMessage = PasswordRule.Message)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; }          // "Client" or "Trainer"

        // Client details (date of birth, gender, height, weight, goals) and trainer details
        // (specialisations, bio) are collected after sign-up by OnboardingController.
    }

    /// <summary>Password rule shared by the registration form (server + unobtrusive client validation).</summary>
    public static class PasswordRule
    {
        public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";
        public const string Message = "Use 8+ characters with upper and lower case letters and a number.";
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
