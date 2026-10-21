
using System;
using System.Collections.Generic;

namespace HealthTrack.ViewModels
{
    public class ClientTrainerViewModel
    {
        public string FirstName { get; set; }

        public bool HasAssignedTrainer { get; set; }

        public int? TrainerId { get; set; }

        public string TrainerName { get; set; }

        public string TrainerBio { get; set; }

        public List<string> Specialisations { get; set; }

        public int ActiveWorkoutPlans { get; set; }

        public int ActiveDietPlans { get; set; }

        public int UpcomingAppointments { get; set; }

        public List<ClientTrainerPlanItem> RecentPlans { get; set; }

        public List<ClientTrainerAppointmentItem> UpcomingSessions { get; set; }

        public ClientTrainerViewModel()
        {
            Specialisations = new List<string>();
            RecentPlans = new List<ClientTrainerPlanItem>();
            UpcomingSessions = new List<ClientTrainerAppointmentItem>();
        }
    }

    public class ClientTrainerPlanItem
    {
        public int PlanId { get; set; }

        public string PlanType { get; set; }

        public string PlanName { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Status { get; set; }
    }

    public class ClientTrainerAppointmentItem
    {
        public int AppointmentId { get; set; }

        public DateTime AppointmentDateTime { get; set; }

        public string Status { get; set; }

        public string Location { get; set; }
    }
}
