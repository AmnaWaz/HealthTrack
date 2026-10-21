
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class ClientNutritionController : ClientBaseController
    {
        [HttpGet]
        public ActionResult Index(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;

            return View(BuildPage(selectedDate));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(ClientMealLogForm form)
        {
            MealLog log = null;

            if (form.MealLogId > 0)
            {
                log = db.Set<MealLog>()
                    .FirstOrDefault(m =>
                        m.MealLogId == form.MealLogId &&
                        m.ClientId == CurrentClientId);

                if (log == null)
                    return HttpNotFound();
            }

            if (!form.Date.HasValue ||
                form.Date.Value.Date > DateTime.Today ||
                form.Date.Value.Date < DateTime.Today.AddYears(-1))
            {
                ModelState.AddModelError(
                    "Date",
                    "Choose a date within the past year."
                );
            }

            var allowedMealTypes = new[]
            {
                "Breakfast",
                "Lunch",
                "Dinner",
                "Snack"
            };

            if (string.IsNullOrWhiteSpace(form.MealType) ||
                !allowedMealTypes.Contains(form.MealType))
            {
                ModelState.AddModelError(
                    "MealType",
                    "Choose a valid meal type."
                );
            }

            if (string.IsNullOrWhiteSpace(form.FoodItem))
            {
                ModelState.AddModelError(
                    "FoodItem",
                    "Enter the food you ate."
                );
            }

            if (form.DietPlanId.HasValue)
            {
                var plan = db.Set<DietPlan>()
                    .FirstOrDefault(p =>
                        p.DietPlanId == form.DietPlanId.Value &&
                        p.ClientId == CurrentClientId);

                if (plan == null)
                {
                    ModelState.AddModelError(
                        "DietPlanId",
                        "Invalid diet plan."
                    );
                }
                else if (form.Date.HasValue &&
                         (form.Date.Value.Date < plan.StartDate.Date ||
                          form.Date.Value.Date > plan.EndDate.Date))
                {
                    ModelState.AddModelError(
                        "DietPlanId",
                        "The meal date is outside the selected plan."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                var selectedDate = form.Date.HasValue &&
                                   form.Date.Value.Date <= DateTime.Today
                    ? form.Date.Value
                    : DateTime.Today;

                var vm = BuildPage(selectedDate);

                vm.LogForm = form;

                return View("Index", vm);
            }

            if (log == null)
            {
                log = new MealLog
                {
                    ClientId = CurrentClientId
                };

                db.Set<MealLog>().Add(log);
            }

            log.Date = form.Date.Value.Date;
            log.DietPlanId = form.DietPlanId;
            log.MealType = form.MealType;
            log.FoodItem = form.FoodItem.Trim();
            log.Calories = form.Calories;

            db.SaveChanges();

            TempData["Success"] = "Meal saved successfully.";

            return RedirectToAction("Index", new
            {
                date = log.Date.ToString("yyyy-MM-dd")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var log = db.Set<MealLog>()
                .FirstOrDefault(m =>
                    m.MealLogId == id &&
                    m.ClientId == CurrentClientId);

            if (log == null)
                return HttpNotFound();

            var date = log.Date;

            db.Set<MealLog>().Remove(log);

            db.SaveChanges();

            TempData["Success"] = "Meal deleted successfully.";

            return RedirectToAction("Index", new
            {
                date = date.ToString("yyyy-MM-dd")
            });
        }

        private ClientNutritionViewModel BuildPage(DateTime selectedDate)
        {
            var clientId = CurrentClientId;
            var today = DateTime.Today;

            var plans = (
                from p in db.Set<DietPlan>()
                join t in db.Set<TrainerProfile>()
                    on p.TrainerId equals t.TrainerId
                join u in db.Set<ApplicationUser>()
                    on t.UserId equals u.UserId
                where p.ClientId == clientId
                orderby p.StartDate descending
                select new
                {
                    p.DietPlanId,
                    p.PlanName,
                    p.Description,
                    p.StartDate,
                    p.EndDate,
                    u.FirstName,
                    u.LastName
                }
            ).ToList();

            var logs = db.Set<MealLog>()
                .Where(m => m.ClientId == clientId)
                .OrderByDescending(m => m.Date)
                .ThenByDescending(m => m.MealLogId)
                .ToList();

            var selectedDayLogs = logs
                .Where(m => m.Date.Date == selectedDate.Date)
                .ToList();

            var vm = new ClientNutritionViewModel
            {
                FirstName = CurrentUser.FirstName,
                SelectedDate = selectedDate.Date,
                ActivePlanCount = plans.Count(p =>
                    p.StartDate.Date <= today &&
                    p.EndDate.Date >= today),
                TotalMealEntries = logs.Count,
                DaysWithMeals = logs
                    .Select(m => m.Date.Date)
                    .Distinct()
                    .Count(),
                TotalRecordedCalories = logs.Sum(m => m.Calories),
                SelectedDayCalories = selectedDayLogs.Sum(m => m.Calories),
                SelectedDayMealCount = selectedDayLogs.Count
            };

            vm.Plans = plans.Select(p => new ClientDietPlanItem
            {
                DietPlanId = p.DietPlanId,
                PlanName = p.PlanName,
                Description = p.Description,
                TrainerName = p.FirstName + " " + p.LastName,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.StartDate.Date > today
                    ? "Upcoming"
                    : p.EndDate.Date < today
                        ? "Ended"
                        : "Active",
                LoggedMeals = logs.Count(m =>
                    m.DietPlanId == p.DietPlanId)
            }).ToList();

            vm.MealLogs = logs.Select(m => new ClientMealLogItem
            {
                MealLogId = m.MealLogId,
                DietPlanId = m.DietPlanId,
                PlanName = m.DietPlanId.HasValue
                    ? plans
                        .Where(p =>
                            p.DietPlanId == m.DietPlanId.Value)
                        .Select(p => p.PlanName)
                        .FirstOrDefault() ?? "Diet plan"
                    : "Independent meal",
                Date = m.Date,
                MealType = m.MealType,
                FoodItem = m.FoodItem,
                Calories = m.Calories
            }).ToList();

            vm.LogForm = new ClientMealLogForm
            {
                Date = selectedDate.Date
            };

            return vm;
        }
    }
}
