// Owner: Person A (Auth / Profiles / Onboarding)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>Junction: which specialisations a trainer offers. Source of truth (TrainerProfile.Specialisation is only a display summary).</summary>
    [Table("TrainerSpecialisations")]
    public class TrainerSpecialisation
    {
        [Key, Column(Order = 0)]
        public int TrainerId { get; set; }

        [Key, Column(Order = 1)]
        public int SpecialisationId { get; set; }

        public int Position { get; set; }

        [ForeignKey("TrainerId")]
        public virtual TrainerProfile Trainer { get; set; }

        [ForeignKey("SpecialisationId")]
        public virtual Specialisation Specialisation { get; set; }
    }
}
