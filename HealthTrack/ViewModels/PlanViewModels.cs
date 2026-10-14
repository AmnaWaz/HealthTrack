using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace HealthTrack.ViewModels
{
    public static class PlanStatusHelper
    {
        public static string Of(DateTime start, DateTime? end, DateTime today)
        {
            if (start.Date > today) return "Upcoming";
            if (end.HasValue && end.Value.Date < today) return "Ended";
            return "Active";
        }

        public static string BadgeClass(string status)
        {
            switch (status)
            {
                case "Active": return "ok";
                case "Ended": return "muted";
                default: return "";
            }
        }
    }

    // used by the create / edit form (shared by workout and diet plans)
    public class PlanFormViewModel
    {
        public int Id { get; set; }
        public string Kind { get; set; }     // "Workout" or "Diet"
        public string Route { get; set; }    // controller name: "WorkoutPlans" or "DietPlans"

        [Required, StringLength(100), Display(Name = "Plan name")]
        public string PlanName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Start date is required."), DataType(DataType.Date), Display(Name = "Start date")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "End date is required."), DataType(DataType.Date), Display(Name = "End date")]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Choose a client."), Display(Name = "Assign to client")]
        public int? ClientId { get; set; }

        public IEnumerable<SelectListItem> Clients { get; set; }
    }

    public class PlanRow
    {
        public int Id { get; set; }
        public string PlanName { get; set; }
        public string Description { get; set; }
        public string ClientName { get; set; }
        public string TrainerName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
    }

    public class PlanIndexViewModel
    {
        public string Kind { get; set; }
        public string Route { get; set; }
        public List<PlanRow> Plans { get; set; } = new List<PlanRow>();
    }

    public class TrainerClientsViewModel
    {
        public List<ClientRow> Mine { get; set; } = new List<ClientRow>();
        public List<ClientRow> Available { get; set; } = new List<ClientRow>();
    }

    public class ClientPlansViewModel
    {
        public string FirstName { get; set; }
        public List<PlanRow> Workout { get; set; } = new List<PlanRow>();
        public List<PlanRow> Diet { get; set; } = new List<PlanRow>();
    }
}
