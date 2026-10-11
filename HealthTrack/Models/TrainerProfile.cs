// Owner: Person A (Auth / Profiles)
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>Extra details for users whose role is Trainer. One profile per user (1:1).</summary>
    public class TrainerProfile
    {
        [Key]
        public int TrainerId { get; set; }

        // FK -> ApplicationUser. The unique index makes this a true one-to-one.
        [Index(IsUnique = true)]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [StringLength(100)]
        public string Specialisation { get; set; }

        [StringLength(500)]
        public string Bio { get; set; }

        public decimal AverageRating { get; set; }

        public virtual ICollection<ClientProfile> Clients { get; set; }
        public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; }
        public virtual ICollection<DietPlan> DietPlans { get; set; }
        public virtual ICollection<Appointment> Appointments { get; set; }
    }
}
