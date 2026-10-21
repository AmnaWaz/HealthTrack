
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class TrainerSelectionViewModel
    {
        public string TrainerName { get; set; }

        public int PendingCount { get; set; }

        public int AcceptedCount { get; set; }

        public int RejectedCount { get; set; }

        public int CancelledCount { get; set; }

        public List<TrainerSelectionItem> Requests { get; set; }

        public TrainerSelectionViewModel()
        {
            Requests = new List<TrainerSelectionItem>();
        }
    }

    public class TrainerSelectionItem
    {
        public int TrainerSelectionRequestId { get; set; }

        public int ClientId { get; set; }

        public string ClientName { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RespondedAt { get; set; }

        public string TrainerNote { get; set; }

        public List<string> FitnessGoals { get; set; }

        public bool CanRespond
        {
            get
            {
                return Status == "Pending";
            }
        }

        public TrainerSelectionItem()
        {
            FitnessGoals = new List<string>();
        }
    }

    public class TrainerSelectionDecisionForm
    {
        [Required]
        public int TrainerSelectionRequestId { get; set; }

        [StringLength(500)]
        public string TrainerNote { get; set; }
    }
}

