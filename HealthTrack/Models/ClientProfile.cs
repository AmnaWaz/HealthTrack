// Owner: Person A (Auth / Profiles)
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>Extra details for users whose role is Client. One profile per user (1:1).</summary>
    public class ClientProfile
    {
        [Key]
        public int ClientId { get; set; }

        // FK -> ApplicationUser (unique = one-to-one)
        [Index(IsUnique = true)]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // FK -> TrainerProfile. Nullable: a new client has no trainer until an admin assigns one.
        public int? TrainerId { get; set; }
        [ForeignKey("TrainerId")]
        public virtual TrainerProfile Trainer { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [StringLength(20)]
        public string Gender { get; set; }

        [Range(50, 260)]
        public decimal Height { get; set; }      // centimetres

        [Range(20, 500)]
        public decimal Weight { get; set; }      // kilograms

        [StringLength(200)]
        public string Goal { get; set; }

        public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; }
        public virtual ICollection<DietPlan> DietPlans { get; set; }
        public virtual ICollection<WorkoutLog> WorkoutLogs { get; set; }
        public virtual ICollection<MealLog> MealLogs { get; set; }
        public virtual ICollection<HealthMetric> HealthMetrics { get; set; }
        public virtual ICollection<Appointment> Appointments { get; set; }
    }
}
