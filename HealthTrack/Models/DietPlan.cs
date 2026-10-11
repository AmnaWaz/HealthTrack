// Owner: Person B (Plans / Logs / Notifications)
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>A personalised diet plan: one trainer, one client (same rule as WorkoutPlan).</summary>
    public class DietPlan
    {
        [Key]
        public int DietPlanId { get; set; }

        public int TrainerId { get; set; }
        [ForeignKey("TrainerId")]
        public virtual TrainerProfile Trainer { get; set; }

        public int ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual ClientProfile Client { get; set; }

        [Required, StringLength(100)]
        public string PlanName { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public virtual ICollection<MealLog> MealLogs { get; set; }
    }
}
