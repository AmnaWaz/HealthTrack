
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class ClientProfileViewModel
    {
        public int ClientId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public DateTime DateJoined { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string Gender { get; set; }

        public decimal Height { get; set; }

        public decimal Weight { get; set; }

        public bool HasAssignedTrainer { get; set; }

        public string TrainerName { get; set; }

        public List<ClientProfileGoalItem> FitnessGoals { get; set; }

        public ClientProfileEditForm EditForm { get; set; }

        public ClientProfileViewModel()
        {
            FitnessGoals = new List<ClientProfileGoalItem>();
            EditForm = new ClientProfileEditForm();
        }
    }

    public class ClientProfileGoalItem
    {
        public int FitnessGoalId { get; set; }

        public string Name { get; set; }

        public int Position { get; set; }
    }

    public class ClientProfileEditForm
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [Range(typeof(decimal), "50", "260")]
        public decimal Height { get; set; }
    }

    public class ClientSettingsViewModel
    {
        public string FullName { get; set; }

        public string Email { get; set; }

        public ClientPasswordChangeForm PasswordForm { get; set; }

        public ClientSettingsViewModel()
        {
            PasswordForm = new ClientPasswordChangeForm();
        }
    }

    public class ClientPasswordChangeForm
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}
