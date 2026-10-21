
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    [Table("TrainerSelectionRequests")]
    public class TrainerSelectionRequest
    {
        [Key]
        public int TrainerSelectionRequestId { get; set; }

        [Required]
        [Index("IX_SelectionRequest_Client")]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual ClientProfile Client { get; set; }

        [Required]
        [Index("IX_SelectionRequest_TrainerStatus", 1)]
        public int TrainerId { get; set; }

        [ForeignKey("TrainerId")]
        public virtual TrainerProfile Trainer { get; set; }

        [Required]
        [StringLength(20)]
        [Index("IX_SelectionRequest_TrainerStatus", 2)]
        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RespondedAt { get; set; }

        [StringLength(500)]
        public string TrainerNote { get; set; }

        public static class Statuses
        {
            public const string Pending = "Pending";
            public const string Accepted = "Accepted";
            public const string Rejected = "Rejected";
            public const string Cancelled = "Cancelled";
        }

        public TrainerSelectionRequest()
        {
            Status = Statuses.Pending;
            CreatedAt = DateTime.Now;
        }
    }
}
