using System;
using System.Collections.Generic;

namespace HealthTrack.ViewModels
{
    public class AdminDailyRow { public DateTime Date { get; set; } public int Workouts { get; set; } public int Meals { get; set; } }
    public class AdminUserRow { public string Name { get; set; } public string Email { get; set; } public string Role { get; set; } public DateTime Joined { get; set; } public bool IsActive { get; set; } }
    public class AdminOverviewVm
    {
        public int Clients { get; set; } public int Trainers { get; set; } public int ActiveUsers { get; set; }
        public int Unassigned { get; set; } public int PendingSelections { get; set; } public int OpenAppointments { get; set; }
        public List<AdminDailyRow> Activity { get; set; } = new List<AdminDailyRow>();
        public List<AdminUserRow> RecentUsers { get; set; } = new List<AdminUserRow>();
    }
    public class AdminExerciseRow { public string Exercise { get; set; } public int Count { get; set; } }
    public class AdminTrainerReportRow { public string Trainer { get; set; } public int Clients { get; set; } public int WorkoutPlans { get; set; } public int DietPlans { get; set; } public int Appointments { get; set; } }
    public class AdminReportsVm
    {
        public DateTime From { get; set; } public DateTime To { get; set; }
        public int WorkoutCount { get; set; } public int MealCount { get; set; } public int ActiveWorkoutClients { get; set; }
        public int ActiveMealClients { get; set; } public int CompletedAppointments { get; set; }
        public List<AdminDailyRow> Daily { get; set; } = new List<AdminDailyRow>();
        public List<AdminExerciseRow> Exercises { get; set; } = new List<AdminExerciseRow>();
        public List<AdminTrainerReportRow> Trainers { get; set; } = new List<AdminTrainerReportRow>();
    }
    public class AdminAccountRow { public int UserId { get; set; } public string Name { get; set; } public string Email { get; set; } public string Role { get; set; } public bool IsActive { get; set; } public DateTime Joined { get; set; } }
    public class AdminUsersVm { public string Query { get; set; } public string Role { get; set; } public string Status { get; set; } public int Page { get; set; } public int Total { get; set; } public List<AdminAccountRow> Users { get; set; } = new List<AdminAccountRow>(); }
    public class AdminTrainerOption { public int TrainerId { get; set; } public string Name { get; set; } }
    public class AdminClientAssignmentRow { public int ClientId { get; set; } public string Client { get; set; } public int? TrainerId { get; set; } public string Trainer { get; set; } }
    public class AdminPendingSelectionRow { public int RequestId { get; set; } public string Client { get; set; } public string Trainer { get; set; } public DateTime Created { get; set; } }
    public class AdminAssignmentsVm { public string Tab { get; set; } public List<AdminTrainerOption> Trainers { get; set; } = new List<AdminTrainerOption>(); public List<AdminClientAssignmentRow> Clients { get; set; } = new List<AdminClientAssignmentRow>(); public List<AdminPendingSelectionRow> Pending { get; set; } = new List<AdminPendingSelectionRow>(); }
}
