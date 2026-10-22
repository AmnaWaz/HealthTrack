// Owner: Person A (Auth / Profiles)
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>Base account for every person who can log in (client, trainer or admin).</summary>
    public class ApplicationUser
    {
        [Key]
        public int UserId { get; set; }

        [Required, StringLength(50)]
        public string FirstName { get; set; }

        [Required, StringLength(50)]
        public string LastName { get; set; }

        [Required, EmailAddress, StringLength(150)]
        [Index(IsUnique = true)]                 // no two accounts may share an email
        public string Email { get; set; }

        /// <summary>Salted hash only. Never store the plain password.</summary>
        [Required, StringLength(256)]
        public string PasswordHash { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(255)]
        public string ProfilePhotoPath { get; set; }

        public DateTime DateJoined { get; set; }

        public bool IsActive { get; set; }

        // FK -> Role
        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }

        public virtual ICollection<Notification> Notifications { get; set; }

        [NotMapped]
        public string FullName { get { return FirstName + " " + LastName; } }
    }
}
