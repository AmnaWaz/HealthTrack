// Owner: Person A (Auth / Profiles / Onboarding)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>Junction: which goals a client chose. The composite key stops the same goal being linked twice.</summary>
    [Table("ClientGoals")]
    public class ClientGoal
    {
        [Key, Column(Order = 0)]
        public int ClientId { get; set; }

        [Key, Column(Order = 1)]
        public int FitnessGoalId { get; set; }

        // Order the user picked them in (0 = first). Used to show the chips in the same order.
        public int Position { get; set; }

        [ForeignKey("ClientId")]
        public virtual ClientProfile Client { get; set; }

        [ForeignKey("FitnessGoalId")]
        public virtual FitnessGoal FitnessGoal { get; set; }
    }
}
