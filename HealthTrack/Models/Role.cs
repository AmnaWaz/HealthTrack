// Owner: Person A (Auth / Profiles)
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    /// <summary>Lookup table for the three user roles: Client, Trainer, Admin.</summary>
    public class Role
    {
        public const string Client = "Client";
        public const string Trainer = "Trainer";
        public const string Admin = "Admin";

        [Key]
        public int RoleId { get; set; }

        [Required, StringLength(30)]
        public string RoleName { get; set; }

        public virtual ICollection<ApplicationUser> Users { get; set; }
    }
}
