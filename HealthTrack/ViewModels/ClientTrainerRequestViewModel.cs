
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class ClientTrainerRequestViewModel
    {
        public string TrainerName { get; set; }

        public bool HasAssignedTrainer { get; set; }

        public int OpenCount { get; set; }

        public int InReviewCount { get; set; }

        public int AnsweredCount { get; set; }

        public int ClosedCount { get; set; }

        public ClientTrainerRequestForm RequestForm { get; set; }

        public List<ClientTrainerRequestItem> Requests { get; set; }

        public ClientTrainerRequestViewModel()
        {
            RequestForm = new ClientTrainerRequestForm();
            Requests = new List<ClientTrainerRequestItem>();
        }
    }

    public class ClientTrainerRequestForm
    {
        [Required(ErrorMessage = "Select a request category.")]
        [StringLength(30)]
        public string Category { get; set; }

        [Required(ErrorMessage = "Select a priority.")]
        [StringLength(10)]
        public string Priority { get; set; }

        [Required(ErrorMessage = "Enter a subject.")]
        [StringLength(100, MinimumLength = 5)]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Describe your request.")]
        [StringLength(1000, MinimumLength = 10)]
        public string Description { get; set; }
    }

    public class ClientTrainerRequestItem
    {
        public int TrainerRequestId { get; set; }

        public string TrainerName { get; set; }

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

        public bool CanClose
        {
            get
            {
                return Status == "Open" ||
                       Status == "In review" ||
                       Status == "Answered";
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
}
