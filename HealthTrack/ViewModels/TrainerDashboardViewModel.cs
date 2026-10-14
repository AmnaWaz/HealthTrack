using System;
using System.Collections.Generic;

namespace HealthTrack.ViewModels
{
    public class TrainerDashboardViewModel
    {
        public string Greeting { get; set; }
        public string FirstName { get; set; }
        public string Specialisation { get; set; }
        public decimal AverageRating { get; set; }

        public int ClientCount { get; set; }
        public int UpcomingAppointmentCount { get; set; }
        public int ActiveWorkoutPlans { get; set; }
        public int ActiveDietPlans { get; set; }
        public int UnreadNotifications { get; set; }

        public List<AppointmentRow> Appointments { get; set; } = new List<AppointmentRow>();
        public List<ClientRow> Clients { get; set; } = new List<ClientRow>();
        public List<NotificationRow> Notifications { get; set; } = new List<NotificationRow>();
    }

    public class AppointmentRow
    {
        public int AppointmentId { get; set; }
        public DateTime When { get; set; }
        public string ClientName { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
    }

    public class ClientRow
    {
        public int ClientId { get; set; }
        public string Name { get; set; }
        public string Initials { get; set; }
        public string Goal { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public int Age { get; set; }
    }

    public class NotificationRow
    {
        public string Message { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
