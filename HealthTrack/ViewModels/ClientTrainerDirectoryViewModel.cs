
using System;
using System.Collections.Generic;

namespace HealthTrack.ViewModels
{
    public class ClientTrainerDirectoryViewModel
    {
        public bool HasAssignedTrainer { get; set; }

        public string AssignedTrainerName { get; set; }

        public bool HasPendingRequest { get; set; }

        public int? PendingTrainerId { get; set; }

        public string PendingTrainerName { get; set; }

        public List<ClientTrainerDirectoryItem> Trainers { get; set; }

        public List<ClientTrainerSelectionHistoryItem> RequestHistory { get; set; }

        public ClientTrainerDirectoryViewModel()
        {
            Trainers = new List<ClientTrainerDirectoryItem>();
            RequestHistory = new List<ClientTrainerSelectionHistoryItem>();
        }
    }

    public class ClientTrainerDirectoryItem
    {
        public int TrainerId { get; set; }

        public string FullName { get; set; }

        public string Bio { get; set; }

        public List<string> Specialisations { get; set; }

        public bool CanRequest { get; set; }

        public bool IsAssigned { get; set; }

        public bool IsPending { get; set; }

        public ClientTrainerDirectoryItem()
        {
            Specialisations = new List<string>();
        }
    }

    public class ClientTrainerSelectionHistoryItem
    {
        public int TrainerSelectionRequestId { get; set; }

        public int TrainerId { get; set; }

        public string TrainerName { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RespondedAt { get; set; }

        public string TrainerNote { get; set; }

        public bool CanCancel
        {
            get { return Status == "Pending"; }
        }
    }
}
