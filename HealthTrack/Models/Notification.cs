// Owner: Person B (Plans / Logs / Notifications)
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>A reminder or message shown to a user (workout, meal or appointment).</summary>
    public class Notification
    {
        public const string WorkoutReminder = "Workout";
        public const string MealReminder = "Meal";
        public const string AppointmentReminder = "Appointment";

        [Key]
        public int NotificationId { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required, StringLength(300)]
        public string Message { get; set; }

        [Required, StringLength(30)]
        public string Type { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
