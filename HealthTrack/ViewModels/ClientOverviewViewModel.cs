

using System;
using System.Collections.Generic;

namespace HealthTrack.ViewModels
{
    public class ClientOverviewViewModel
    {
        public string FirstName { get; set; }
        public string FullName { get; set; }

        public int TrainingDaysThisWeek { get; set; }
        public int WorkoutSessionsThisWeek { get; set; }
        public int MealsLoggedThisWeek { get; set; }

        public decimal? LatestWeight { get; set; }
        public DateTime? LatestWeightDate { get; set; }
        public bool WeightIsFromProfile { get; set; }

        public List<string> FitnessGoals { get; set; }

        public DateTime? NextSessionDate { get; set; }
        public string NextSessionTrainer { get; set; }
        public string NextSessionLocation { get; set; }
        public string NextSessionNotes { get; set; }
        public string NextSessionStatus { get; set; }

        public List<ClientTrainingWeekViewModel> TrainingWeeks { get; set; }

        public ClientOverviewViewModel()
        {
            FitnessGoals = new List<string>();
            TrainingWeeks = new List<ClientTrainingWeekViewModel>();
        }
    }

    public class ClientTrainingWeekViewModel
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public int TrainingDays { get; set; }
        public int WorkoutSessions { get; set; }
        public int TotalExercises { get; set; }
        public decimal TotalMinutes { get; set; }
        public bool IsCurrentWeek { get; set; }
    }
}

