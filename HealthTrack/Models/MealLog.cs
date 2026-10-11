// Owner: Person B (Plans / Logs / Notifications)
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>One meal entry that a client records.</summary>
    public class MealLog
    {
        [Key]
        public int MealLogId { get; set; }

        public int ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual ClientProfile Client { get; set; }

        // Nullable: a meal may be logged without following a diet plan.
        public int? DietPlanId { get; set; }
        [ForeignKey("DietPlanId")]
        public virtual DietPlan DietPlan { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required, StringLength(30)]
        public string MealType { get; set; }         // Breakfast, Lunch, Dinner, Snack

        [Required, StringLength(150)]
        public string FoodItem { get; set; }

        [Range(0, 10000)]
        public decimal Calories { get; set; }
    }
}
