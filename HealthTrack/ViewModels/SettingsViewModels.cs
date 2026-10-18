using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Enter your current password."), DataType(DataType.Password), Display(Name = "Current password")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Enter a new password."), MinLength(6, ErrorMessage = "Use at least 6 characters."), DataType(DataType.Password), Display(Name = "New password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm the new password."), DataType(DataType.Password), Display(Name = "Confirm new password")]
        [System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
