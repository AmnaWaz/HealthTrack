// Owner: Person A (Auth / Profiles / Onboarding)
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>
    /// One selectable fitness goal. Predefined goals are seeded; custom goals are created by
    /// clients during onboarding (IsPredefined = false) and are only suggested to their creator.
    /// </summary>
    [Table("FitnessGoals")]
    public class FitnessGoal
    {
        [Key]
        public int FitnessGoalId { get; set; }

        [Required, StringLength(50)]
        [Index("UX_FitnessGoals_Name", IsUnique = true)]   // case-insensitive in SQL Server: no duplicates
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
