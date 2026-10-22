using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace HealthTrack.ViewModels
{
    // ---------- appointments ----------
    public class AppointmentFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Choose a client."), Display(Name = "Client")]
        public int? ClientId { get; set; }

        [Required(ErrorMessage = "Choose a date and time."), Display(Name = "Date and time")]
        public DateTime? When { get; set; }

        [StringLength(150)]
        public string Location { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public IEnumerable<SelectListItem> Clients { get; set; }
    }

    public class AppointmentsIndexViewModel
    {
        public string Filter { get; set; }
        public int CountPending { get; set; }
        public int CountUpcoming { get; set; }
        public int CountNeedsUpdate { get; set; }
        public int CountCompleted { get; set; }
        public int CountCancelled { get; set; }
        public int CountAll { get; set; }
        public List<AppointmentRow> Items { get; set; } = new List<AppointmentRow>();
    }

    // ---------- feedback ----------
    public class FeedbackPageViewModel
    {
        [Required(ErrorMessage = "Choose a client."), Display(Name = "Client")]
        public int? ClientId { get; set; }

        [Required(ErrorMessage = "Write your feedback."), StringLength(300), Display(Name = "Feedback")]
        public string Message { get; set; }

        public decimal AverageRating { get; set; }
        public IEnumerable<SelectListItem> Clients { get; set; }
        public List<FeedbackRow> Sent { get; set; } = new List<FeedbackRow>();
    }

    public class FeedbackRow
    {
        public string ClientName { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ---------- reports ----------
    public class ReportRow
    {
        public string ClientName { get; set; }
        public string Initials { get; set; }
        public int Workouts { get; set; }
        public decimal CaloriesBurned { get; set; }
        public decimal? AvgCaloriesIn { get; set; }
        public decimal? WeightChange { get; set; }
        public DateTime? LastActivity { get; set; }
        public int Pct { get; set; }
    }

    public class WeekBar
    {
        public string Label { get; set; }
        public int Count { get; set; }
        public int Pct { get; set; }
    }

    public class ReportsViewModel
    {
        public int Days { get; set; }
        public int ClientCount { get; set; }
        public int ActiveClients { get; set; }
        public int TotalWorkouts { get; set; }
        public decimal TotalCaloriesBurned { get; set; }
        public int CompletedAppointments { get; set; }
        public List<ReportRow> Rows { get; set; } = new List<ReportRow>();
        public List<WeekBar> Weeks { get; set; } = new List<WeekBar>();
    }

    // ---------- notifications ----------
    public class NotificationItem
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class NotificationsPageViewModel
    {
        public string Filter { get; set; }
        public int Total { get; set; }
        public int Unread { get; set; }
        public List<NotificationItem> Items { get; set; } = new List<NotificationItem>();
    }

    // ---------- profile ----------
    public class ProfileViewModel
    {
        [Required, StringLength(50), Display(Name = "First name")]
        public string FirstName { get; set; }

        [Required, StringLength(50), Display(Name = "Last name")]
        public string LastName { get; set; }

        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required."), Phone, StringLength(25)]
        public string Phone { get; set; }

        [StringLength(100)]
        public string Specialisation { get; set; }

        [StringLength(500)]
        public string Bio { get; set; }
    }
}
