// Owner: Person B (Plans / Logs / Notifications)
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>One exercise entry that a client records.</summary>
    public class WorkoutLog
    {
        [Key]
        public int WorkoutLogId { get; set; }

        public int ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual ClientProfile Client { get; set; }

        // Nullable: a client may log a workout that is not part of any plan.
        public int? WorkoutPlanId { get; set; }
        [ForeignKey("WorkoutPlanId")]
        public virtual WorkoutPlan WorkoutPlan { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required, StringLength(100)]
        public string Exercise { get; set; }

        [Range(0, 600)]
        public decimal Duration { get; set; }        // minutes

        [Range(0, 100)]
        public int Sets { get; set; }

        [Range(0, 1000)]
        public int Reps { get; set; }

        [Range(0, 10000)]
        public decimal CaloriesBurned { get; set; }
    }
}
