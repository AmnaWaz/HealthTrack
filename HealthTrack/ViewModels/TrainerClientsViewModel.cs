using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HealthTrack.ViewModels
{
    public class TrainerClientViewModel
    {// Summary metrics for the top of the client management page
        public int TotalClients { get; set; }
        public int ActiveClientsCount { get; set; }
        public int PendingCheckInsCount { get; set; }

        // Filter and Search parameters
        public string SearchTerm { get; set; }
        public string SelectedGoal { get; set; }
        public List<string> AvailableGoals { get; set; } = new List<string>();

        // The list of client summary rows for the table/grid
        public List<TrainerClientRowViewModel> Clients { get; set; } = new List<TrainerClientRowViewModel>();

        // Pagination metadata
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class TrainerClientRowViewModel
    {
        public int ClientId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Initials { get; set; }
        public string ProfilePictureUrl { get; set; }

        // Physical & Fitness Stats
        public string Goal { get; set; }
        public decimal CurrentWeight { get; set; }
        public decimal TargetWeight { get; set; }
        public int Age { get; set; }
        public string Status { get; set; } // e.g., "Active", "Inactive", "Pending"

        // Plan Assignments
        public bool HasActiveWorkoutPlan { get; set; }
        public bool HasActiveDietPlan { get; set; }
        public DateTime? LastSessionDate { get; set; }
        public DateTime? NextAppointmentDate { get; set; }
    }
}