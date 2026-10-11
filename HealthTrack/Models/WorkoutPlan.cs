// Owner: Person B (Plans / Logs / Notifications)
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>
    /// A personalised workout plan. Each plan is created by ONE trainer for ONE client
    /// (a client can have many plans, a plan belongs to exactly one client).
    /// </summary>
    public class WorkoutPlan
    {
        [Key]
        public int WorkoutPlanId { get; set; }

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

        public virtual ICollection<WorkoutLog> WorkoutLogs { get; set; }
    }
}
