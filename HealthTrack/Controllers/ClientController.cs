using System;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientController : ClientBaseController
    {



        public ActionResult Overview()
        {
            var email = User.Identity.Name;

            var user = db.Set<ApplicationUser>()
                .FirstOrDefault(u => u.Email == email && u.IsActive);

            if (user == null)
                return HttpNotFound("User not found.");

            var client = db.Set<ClientProfile>()
                .FirstOrDefault(c => c.UserId == user.UserId);

            if (client == null)
                return RedirectToAction("Index", "Onboarding");

            var today = DateTime.Today;
            var weekStart = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            var weekEnd = weekStart.AddDays(7);
            var clientId = client.ClientId;

            var workoutLogs = db.WorkoutLogs
                .Where(w => w.ClientId == clientId &&
                            w.Date >= weekStart &&
                            w.Date < weekEnd)
                .Select(w => new
                {
                    w.Date,
                    w.WorkoutPlanId
                })
                .ToList();

            var trainingDays = workoutLogs
                .Select(w => w.Date.Date)
                .Distinct()
                .Count();

            var workoutSessions = workoutLogs
                .GroupBy(w => new
                {
                    Date = w.Date.Date,
                    w.WorkoutPlanId
                })
                .Count();

            var mealsLogged = db.MealLogs
                .Count(m => m.ClientId == clientId &&
                            m.Date >= weekStart &&
                            m.Date < weekEnd);

            var latestMetric = db.HealthMetrics
                .Where(m => m.ClientId == clientId)
                .OrderByDescending(m => m.DateRecorded)
                .ThenByDescending(m => m.MetricId)
                .FirstOrDefault();

            var fitnessGoals = (
                from cg in db.ClientGoals
                join fg in db.FitnessGoals
                    on cg.FitnessGoalId equals fg.FitnessGoalId
                where cg.ClientId == clientId
                orderby cg.Position
                select fg.Name
            ).ToList();

            var vm = new ClientOverviewViewModel
            {
                FirstName = user.FirstName,
                FullName = user.FullName,
                TrainingDaysThisWeek = trainingDays,
                WorkoutSessionsThisWeek = workoutSessions,
                MealsLoggedThisWeek = mealsLogged,
                LatestWeight = latestMetric != null
                    ? latestMetric.Weight
                    : client.Weight,
                LatestWeightDate = latestMetric != null
                    ? (DateTime?)latestMetric.DateRecorded
                    : null,
                WeightIsFromProfile = latestMetric == null,
                FitnessGoals = fitnessGoals
            };

            return View(vm);
        }

        public ActionResult Plans()
        {
            var email = User.Identity.Name;
            var user = db.Set<ApplicationUser>().FirstOrDefault(x => x.Email == email);
            if (user == null) return HttpNotFound("User not found.");
            var userId = user.UserId;

            var client = db.Set<ClientProfile>().FirstOrDefault(c => c.UserId == userId);
            if (client == null) return HttpNotFound("No client profile exists for this account.");
            var cid = client.ClientId;
            var today = DateTime.Today;

            var workoutRaw = (from p in db.Set<WorkoutPlan>()
                              join t in db.Set<TrainerProfile>() on p.TrainerId equals t.TrainerId
                              join u in db.Set<ApplicationUser>() on t.UserId equals u.UserId
                              where p.ClientId == cid
                              orderby p.StartDate descending
                              select new { Id = p.WorkoutPlanId, p.PlanName, p.Description, p.StartDate, p.EndDate, u.FirstName, u.LastName })
                             .ToList();

            var dietRaw = (from p in db.Set<DietPlan>()
                           join t in db.Set<TrainerProfile>() on p.TrainerId equals t.TrainerId
                           join u in db.Set<ApplicationUser>() on t.UserId equals u.UserId
                           where p.ClientId == cid
                           orderby p.StartDate descending
                           select new { Id = p.DietPlanId, p.PlanName, p.Description, p.StartDate, p.EndDate, u.FirstName, u.LastName })
                          .ToList();

            var vm = new ClientPlansViewModel { FirstName = user.FirstName };

            vm.Workout = workoutRaw.Select(r =>
            {
                DateTime? end = r.EndDate;
                return new PlanRow
                {
                    Id = r.Id,
                    PlanName = r.PlanName,
                    Description = r.Description,
                    StartDate = r.StartDate,
                    EndDate = end,
                    TrainerName = r.FirstName + " " + r.LastName,
                    Status = PlanStatusHelper.Of(r.StartDate, end, today)
                };
            }).ToList();

            vm.Diet = dietRaw.Select(r =>
            {
                DateTime? end = r.EndDate;
                return new PlanRow
                {
                    Id = r.Id,
                    PlanName = r.PlanName,
                    Description = r.Description,
                    StartDate = r.StartDate,
                    EndDate = end,
                    TrainerName = r.FirstName + " " + r.LastName,
                    Status = PlanStatusHelper.Of(r.StartDate, end, today)
                };
            }).ToList();

            return View(vm);
        }

    }
}
