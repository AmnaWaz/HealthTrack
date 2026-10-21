
using System;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class ClientTrainingController : ClientBaseController
    {
        [HttpGet]
        public ActionResult Index(DateTime? date)
        {
            return View(BuildPage(date ?? DateTime.Today));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(ClientWorkoutLogForm form)
        {
            if (!form.Date.HasValue ||
                form.Date.Value.Date > DateTime.Today ||
                form.Date.Value.Date < DateTime.Today.AddYears(-1))
            {
                ModelState.AddModelError("Date", "Choose a date within the past year.");
            }

            WorkoutPlan selectedPlan = null;

            if (form.WorkoutPlanId.HasValue)
            {
                selectedPlan = db.Set<WorkoutPlan>()
                    .FirstOrDefault(p =>
                        p.WorkoutPlanId == form.WorkoutPlanId.Value &&
                        p.ClientId == CurrentClientId);

                if (selectedPlan == null)
                {
                    ModelState.AddModelError("WorkoutPlanId", "Invalid workout plan.");
                }
                else if (form.Date.HasValue &&
                    (form.Date.Value.Date < selectedPlan.StartDate.Date ||
                    form.Date.Value.Date > selectedPlan.EndDate.Date))
                {
                    ModelState.AddModelError("WorkoutPlanId", "The date is outside the selected plan.");
                }
            }

            WorkoutLog log = null;

            if (form.WorkoutLogId > 0)
            {
                log = db.Set<WorkoutLog>()
                    .FirstOrDefault(w =>
                        w.WorkoutLogId == form.WorkoutLogId &&
                        w.ClientId == CurrentClientId);

                if (log == null)
                    return HttpNotFound();
            }

            if (!ModelState.IsValid)
            {
                var vm = BuildPage(form.Date ?? DateTime.Today);
                vm.LogForm = form;
                vm.LogForm.AvailablePlans = GetAvailablePlans();
                return View("Index", vm);
            }

            if (log == null)
            {
                log = new WorkoutLog
                {
                    ClientId = CurrentClientId
                };

                db.Set<WorkoutLog>().Add(log);
            }

            log.WorkoutPlanId = form.WorkoutPlanId;
            log.Date = form.Date.Value.Date;
            log.Exercise = form.Exercise.Trim();
            log.Sets = form.Sets;
            log.Reps = form.Reps;
            log.Duration = form.Duration;
            log.CaloriesBurned = form.CaloriesBurned;

            db.SaveChanges();

            TempData["Success"] = "Workout saved successfully.";

            return RedirectToAction("Index", new
            {
                date = log.Date.ToString("yyyy-MM-dd")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var log = db.Set<WorkoutLog>()
                .FirstOrDefault(w =>
                    w.WorkoutLogId == id &&
                    w.ClientId == CurrentClientId);

            if (log == null)
                return HttpNotFound();

            var date = log.Date;

            db.Set<WorkoutLog>().Remove(log);
            db.SaveChanges();

            TempData["Success"] = "Workout deleted successfully.";

            return RedirectToAction("Index", new
            {
                date = date.ToString("yyyy-MM-dd")
            });
        }

        private ClientTrainingViewModel BuildPage(DateTime selectedDate)
        {
            var today = DateTime.Today;
            var clientId = CurrentClientId;

            var plans = (
                from p in db.Set<WorkoutPlan>()
                join t in db.Set<TrainerProfile>()
                    on p.TrainerId equals t.TrainerId
                join u in db.Set<ApplicationUser>()
                    on t.UserId equals u.UserId
                where p.ClientId == clientId
                orderby p.StartDate descending
                select new
                {
                    p.WorkoutPlanId,
                    p.PlanName,
                    p.Description,
                    p.StartDate,
                    p.EndDate,
                    u.FirstName,
                    u.LastName
                }
            ).ToList();

            var logs = db.Set<WorkoutLog>()
                .Where(w => w.ClientId == clientId)
                .OrderByDescending(w => w.Date)
                .ThenByDescending(w => w.WorkoutLogId)
                .ToList();

            var vm = new ClientTrainingViewModel
            {
                FirstName = CurrentUser.FirstName,
                SelectedDate = selectedDate.Date,
                TotalTrainingDays = logs
                    .Select(w => w.Date.Date)
                    .Distinct()
                    .Count(),
                TotalExerciseEntries = logs.Count,
                TotalMinutes = logs.Sum(w => w.Duration),
                TotalCaloriesBurned = logs.Sum(w => w.CaloriesBurned),
                ActivePlanCount = plans.Count(p =>
    p.StartDate.Date <= today &&
    p.EndDate.Date >= today)


            };
            vm.Plans = plans.Select(p => new ClientWorkoutPlanItem
            {
                WorkoutPlanId = p.WorkoutPlanId,
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
                ExerciseEntries = logs.Count(w =>
                    w.WorkoutPlanId == p.WorkoutPlanId)
            }).ToList();

            vm.WorkoutLogs = logs.Select(w => new ClientWorkoutLogItem
            {
                WorkoutLogId = w.WorkoutLogId,
                WorkoutPlanId = w.WorkoutPlanId,
                PlanName = w.WorkoutPlanId.HasValue
                    ? plans.Where(p => p.WorkoutPlanId == w.WorkoutPlanId.Value)
                        .Select(p => p.PlanName)
                        .FirstOrDefault() ?? "Workout plan"
                    : "Independent workout",
                Date = w.Date,
                Exercise = w.Exercise,
                Sets = w.Sets,
                Reps = w.Reps,
                Duration = w.Duration,
                CaloriesBurned = w.CaloriesBurned
            }).ToList();

            vm.LogForm = new ClientWorkoutLogForm
            {
                Date = selectedDate.Date,
                AvailablePlans = GetAvailablePlans()
            };

            return vm;
        }

        private System.Collections.Generic.IEnumerable<SelectListItem> GetAvailablePlans()
        {
            var plans = db.Set<WorkoutPlan>()
                .Where(p => p.ClientId == CurrentClientId)
                .OrderByDescending(p => p.StartDate)
                .Select(p => new
                {
                    p.WorkoutPlanId,
                    p.PlanName
                })
                .ToList();

            return plans.Select(p => new SelectListItem
            {
                Value = p.WorkoutPlanId.ToString(),
                Text = p.PlanName
            }).ToList();
        }
    }
}
