// Owner: Person A (Auth / Profiles / Onboarding)
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>
    /// One selectable trainer specialisation. Predefined ones are seeded; custom ones are created by
    /// trainers during onboarding or on the Profile page (IsPredefined = false) and are only suggested to their creator.
    /// </summary>
    [Table("Specialisations")]
    public class Specialisation
    {
        [Key]
        public int SpecialisationId { get; set; }

        [Required, StringLength(50)]
        [Index("UX_Specialisations_Name", IsUnique = true)]   // case-insensitive in SQL Server: no duplicates
        public string Name { get; set; }

        public bool IsPredefined { get; set; }

        public int SortOrder { get; set; }

        // Who typed this custom option (null for predefined options).
        public int? CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public virtual ApplicationUser CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
