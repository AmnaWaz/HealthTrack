
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class ClientProgressViewModel
    {
        public string FirstName { get; set; }

        public decimal HeightCm { get; set; }

        public decimal CurrentWeight { get; set; }

        public decimal? CurrentBMI { get; set; }

        public decimal? CurrentBodyFat { get; set; }

        public DateTime? LatestMeasurementDate { get; set; }

        public bool WeightIsFromProfile { get; set; }

        public int TotalMeasurements { get; set; }

        public int TotalTrainingDays { get; set; }

        public int TotalExerciseEntries { get; set; }

        public decimal TotalWorkoutMinutes { get; set; }

        public decimal TotalRecordedCaloriesBurned { get; set; }

        public decimal? WeightChange { get; set; }

        public List<ClientMeasurementItem> Measurements { get; set; }

        public List<ClientWorkoutProgressItem> WorkoutActivity { get; set; }

        public ClientMeasurementForm MeasurementForm { get; set; }

        public ClientProgressViewModel()
        {
            Measurements = new List<ClientMeasurementItem>();
            WorkoutActivity = new List<ClientWorkoutProgressItem>();
            MeasurementForm = new ClientMeasurementForm();
        }
    }

    public class ClientMeasurementItem
    {
        public int MetricId { get; set; }

        public DateTime DateRecorded { get; set; }

        public decimal Weight { get; set; }

        public decimal BMI { get; set; }

        public decimal? BodyFat { get; set; }
    }

    public class ClientWorkoutProgressItem
    {
        public DateTime Date { get; set; }

        public int ExerciseEntries { get; set; }

        public decimal TotalMinutes { get; set; }

        public decimal RecordedCaloriesBurned { get; set; }
    }

    public class ClientMeasurementForm
    {
        public int MetricId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? DateRecorded { get; set; }

        [Range(typeof(decimal), "20", "500")]
        public decimal Weight { get; set; }

        [Range(typeof(decimal), "0", "80")]
        public decimal? BodyFat { get; set; }

        public ClientMeasurementForm()
        {
            DateRecorded = DateTime.Today;
        }
    }
}
