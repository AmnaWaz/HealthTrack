
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class TrainerRequestsViewModel
    {
        public string TrainerName { get; set; }

        public string Filter { get; set; }

        public int TotalCount { get; set; }

        public int OpenCount { get; set; }

        public int InReviewCount { get; set; }

        public int AnsweredCount { get; set; }

        public int ClosedCount { get; set; }

        public int HighPriorityCount { get; set; }

        public int UrgentReviewCount { get; set; }

        public List<TrainerRequestInboxItem> Requests { get; set; }

        public TrainerRequestsViewModel()
        {
            Filter = "all";
            Requests = new List<TrainerRequestInboxItem>();
        }
    }

    public class TrainerRequestInboxItem
    {
        public int TrainerRequestId { get; set; }

        public int ClientId { get; set; }

        public string ClientName { get; set; }

        public string Category { get; set; }

        public string Priority { get; set; }

        public string Subject { get; set; }

        public string Description { get; set; }

        public string Status { get; set; }

        public string TrainerResponse { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? RespondedAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        public string ClosedByRole { get; set; }

        public string AiSuggestedPriority { get; set; }

        public string AiReason { get; set; }

        public bool RequiresUrgentReview { get; set; }

        public string AiAnalysisSource { get; set; }

        public bool IsHighPriority
        {
            get
            {
                return Priority == "High" ||
                       AiSuggestedPriority == "High";
            }
        }

        public bool HasPriorityAnalysis
        {
            get
            {
                return !string.IsNullOrWhiteSpace(AiAnalysisSource);
            }
        }

        public bool CanReview
        {
            get
            {
                return Status == "Open";
            }
        }

        public bool CanRespond
        {
            get
            {
                return Status == "Open" ||
                       Status == "In review";
            }
        }

        public bool CanClose
        {
            get
            {
                return Status == "Answered";
            }
        }

        public bool HasResponse
        {
            get
            {
                return !string.IsNullOrWhiteSpace(TrainerResponse);
            }
        }
    }

    public class TrainerRequestResponseForm
    {
        [Required]
        public int TrainerRequestId { get; set; }

        [Required(ErrorMessage = "Enter a response for your client.")]
        [StringLength(1000, MinimumLength = 5)]
        public string TrainerResponse { get; set; }
    }
}
