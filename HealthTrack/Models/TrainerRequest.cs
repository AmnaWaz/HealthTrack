
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    [Table("TrainerRequests")]
    public class TrainerRequest
    {
        [Key]
        public int TrainerRequestId { get; set; }

        [Required]
        [Index("IX_TrainerRequest_Client")]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual ClientProfile Client { get; set; }

        [Required]
        [Index("IX_TrainerRequest_TrainerStatus", 1)]
        public int TrainerId { get; set; }

        [ForeignKey("TrainerId")]
        public virtual TrainerProfile Trainer { get; set; }

        [Required]
        [StringLength(30)]
        public string Category { get; set; }

        [Required]
        [StringLength(10)]
        public string Priority { get; set; }

        [Required]
        [StringLength(100)]
        public string Subject { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [StringLength(20)]
        [Index("IX_TrainerRequest_TrainerStatus", 2)]
        public string Status { get; set; }

        [StringLength(1000)]
        public string TrainerResponse { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? RespondedAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        [StringLength(10)]
        public string ClosedByRole { get; set; }

        public static class Categories
        {
            public const string MedicalConcern = "Medical concern";
            public const string WorkoutDifficulty = "Workout difficulty";
            public const string DietaryConcern = "Dietary concern";
            public const string FitnessGoal = "Fitness goal";
            public const string Availability = "Availability";
            public const string PlanAdjustment = "Plan adjustment";
            public const string GeneralQuestion = "General question";
        }

        public static class Priorities
        {
            public const string Normal = "Normal";
            public const string High = "High";
        }

        public static class Statuses
        {
            public const string Open = "Open";
            public const string InReview = "In review";
            public const string Answered = "Answered";
            public const string Closed = "Closed";
        }

        public TrainerRequest()
        {
            Category = Categories.GeneralQuestion;
            Priority = Priorities.Normal;
            Status = Statuses.Open;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}
