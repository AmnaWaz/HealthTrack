
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class ClientNutritionViewModel
    {
        public string FirstName { get; set; }

        public DateTime SelectedDate { get; set; }

        public int ActivePlanCount { get; set; }

        public int TotalMealEntries { get; set; }

        public int DaysWithMeals { get; set; }

        public decimal TotalRecordedCalories { get; set; }

        public decimal SelectedDayCalories { get; set; }

        public int SelectedDayMealCount { get; set; }

        public List<ClientDietPlanItem> Plans { get; set; }

        public List<ClientMealLogItem> MealLogs { get; set; }

        public ClientMealLogForm LogForm { get; set; }

        public ClientNutritionViewModel()
        {
            SelectedDate = DateTime.Today;
            Plans = new List<ClientDietPlanItem>();
            MealLogs = new List<ClientMealLogItem>();
            LogForm = new ClientMealLogForm();
        }
    }

    public class ClientDietPlanItem
    {
        public int DietPlanId { get; set; }

        public string PlanName { get; set; }

        public string Description { get; set; }

        public string TrainerName { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Status { get; set; }

        public int LoggedMeals { get; set; }
    }

    public class ClientMealLogItem
    {
        public int MealLogId { get; set; }

        public int? DietPlanId { get; set; }

        public string PlanName { get; set; }

        public DateTime Date { get; set; }

        public string MealType { get; set; }

        public string FoodItem { get; set; }

        public decimal Calories { get; set; }
    }

    public class ClientMealLogForm
    {
        public int MealLogId { get; set; }

        public int? DietPlanId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? Date { get; set; }

        [Required]
        [StringLength(30)]
        public string MealType { get; set; }

        [Required]
        [StringLength(150)]
        public string FoodItem { get; set; }

        [Range(typeof(decimal), "0", "10000")]
        public decimal Calories { get; set; }

        public ClientMealLogForm()
        {
            Date = DateTime.Today;
        }
    }
}
