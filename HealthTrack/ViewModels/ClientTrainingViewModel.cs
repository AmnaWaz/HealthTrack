
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace HealthTrack.ViewModels
{
    public class ClientTrainingViewModel
    {
        public string FirstName { get; set; }

        public DateTime SelectedDate { get; set; }

        public int TotalTrainingDays { get; set; }

        public int TotalExerciseEntries { get; set; }

        public decimal TotalMinutes { get; set; }

        public decimal TotalCaloriesBurned { get; set; }

        public int ActivePlanCount { get; set; }

        public List<ClientWorkoutPlanItem> Plans { get; set; }

        public List<ClientWorkoutLogItem> WorkoutLogs { get; set; }

        public ClientWorkoutLogForm LogForm { get; set; }

        public ClientTrainingViewModel()
        {
            Plans = new List<ClientWorkoutPlanItem>();
            WorkoutLogs = new List<ClientWorkoutLogItem>();
            LogForm = new ClientWorkoutLogForm();
            SelectedDate = DateTime.Today;
        }
    }

    public class ClientWorkoutPlanItem
    {
        public int WorkoutPlanId { get; set; }

        public string PlanName { get; set; }

        public string Description { get; set; }

        public string TrainerName { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Status { get; set; }

        public int ExerciseEntries { get; set; }
    }

    public class ClientWorkoutLogItem
    {
        public int WorkoutLogId { get; set; }

        public int? WorkoutPlanId { get; set; }

        public string PlanName { get; set; }

        public DateTime Date { get; set; }

        public string Exercise { get; set; }

        public int Sets { get; set; }

        public int Reps { get; set; }

        public decimal Duration { get; set; }

        public decimal CaloriesBurned { get; set; }
    }

    public class ClientWorkoutLogForm
    {
        public int WorkoutLogId { get; set; }

        public int? WorkoutPlanId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? Date { get; set; }

        [Required]
        [StringLength(100)]
        public string Exercise { get; set; }

        [Range(0, 100)]
        public int Sets { get; set; }

        [Range(0, 1000)]
        public int Reps { get; set; }

        [Range(typeof(decimal), "0", "600")]
        public decimal Duration { get; set; }

        [Range(typeof(decimal), "0", "10000")]
        public decimal CaloriesBurned { get; set; }

        public IEnumerable<SelectListItem> AvailablePlans { get; set; }

        public ClientWorkoutLogForm()
        {
            Date = DateTime.Today;
            AvailablePlans = new List<SelectListItem>();
        }
    }
}
