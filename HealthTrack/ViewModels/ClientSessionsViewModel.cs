
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class ClientSessionsViewModel
    {
        public string FirstName { get; set; }

        public bool HasAssignedTrainer { get; set; }

        public string TrainerName { get; set; }

        public int PendingCount { get; set; }

        public int UpcomingCount { get; set; }

        public int CompletedCount { get; set; }

        public int CancelledCount { get; set; }

        public List<ClientAppointmentItem> UpcomingAppointments { get; set; }

        public List<ClientAppointmentItem> PendingAppointments { get; set; }

        public List<ClientAppointmentItem> AppointmentHistory { get; set; }

        public ClientAppointmentRequestForm RequestForm { get; set; }

        public ClientSessionsViewModel()
        {
            UpcomingAppointments = new List<ClientAppointmentItem>();
            PendingAppointments = new List<ClientAppointmentItem>();
            AppointmentHistory = new List<ClientAppointmentItem>();
            RequestForm = new ClientAppointmentRequestForm();
        }
    }

    public class ClientAppointmentItem
    {
        public int AppointmentId { get; set; }

        public int TrainerId { get; set; }

        public string TrainerName { get; set; }

        public DateTime AppointmentDateTime { get; set; }

        public string Status { get; set; }

        public string Location { get; set; }

        public string Notes { get; set; }

        public bool CanWithdraw { get; set; }

        public bool IsUpcoming { get; set; }
    }

    public class ClientAppointmentRequestForm
    {
        [Required(ErrorMessage = "Choose an appointment date.")]
        [DataType(DataType.Date)]
        public DateTime? Date { get; set; }

        [Required(ErrorMessage = "Choose an appointment time.")]
        [DataType(DataType.Time)]
        public TimeSpan? Time { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public ClientAppointmentRequestForm()
        {
            Date = DateTime.Today.AddDays(1);
            Time = new TimeSpan(9, 0, 0);
        }
    }
}
