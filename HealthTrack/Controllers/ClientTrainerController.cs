
using System;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class ClientTrainerController : ClientBaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            var today = DateTime.Today;
            var now = DateTime.Now;

            var vm = new ClientTrainerViewModel
            {
                FirstName = CurrentUser.FirstName,
                HasAssignedTrainer = CurrentClient.TrainerId.HasValue,
                TrainerId = CurrentClient.TrainerId
            };

            if (!CurrentClient.TrainerId.HasValue)
            {
                return View(vm);
            }

            var trainerId = CurrentClient.TrainerId.Value;
            var clientId = CurrentClientId;

            var trainer = (
                from t in db.Set<TrainerProfile>()
                join u in db.Set<ApplicationUser>()
                    on t.UserId equals u.UserId
                where t.TrainerId == trainerId
                select new
                {
                    t.TrainerId,
                    t.Bio,
                    t.Specialisation,
                    t.AverageRating,
                    u.ProfilePhotoPath,
                    u.FirstName,
                    u.LastName
                }
            ).FirstOrDefault();

            if (trainer == null)
            {
                vm.HasAssignedTrainer = false;
                vm.TrainerId = null;
                return View(vm);
            }

            vm.TrainerName = (
                (trainer.FirstName ?? "") + " " +
                (trainer.LastName ?? "")
            ).Trim();

            vm.TrainerBio = trainer.Bio;
            vm.TrainerPhotoPath = trainer.ProfilePhotoPath;
            vm.TrainerAverageRating = trainer.AverageRating;

            vm.Specialisations = (
                from ts in db.Set<TrainerSpecialisation>()
                join s in db.Set<Specialisation>()
                    on ts.SpecialisationId equals s.SpecialisationId
                where ts.TrainerId == trainerId
                orderby ts.Position, s.SortOrder, s.Name
                select s.Name
            ).ToList();

            if (!vm.Specialisations.Any() &&
                !string.IsNullOrWhiteSpace(trainer.Specialisation))
            {
                vm.Specialisations.Add(trainer.Specialisation);
            }

            var workoutPlans = db.Set<WorkoutPlan>()
                .Where(p =>
                    p.ClientId == clientId &&
                    p.TrainerId == trainerId)
                .Select(p => new
                {
                    p.WorkoutPlanId,
                    p.PlanName,
                    p.StartDate,
                    p.EndDate
                })
                .ToList();

            var dietPlans = db.Set<DietPlan>()
                .Where(p =>
                    p.ClientId == clientId &&
                    p.TrainerId == trainerId)
                .Select(p => new
                {
                    p.DietPlanId,
                    p.PlanName,
                    p.StartDate,
                    p.EndDate
                })
                .ToList();

            vm.ActiveWorkoutPlans = workoutPlans.Count(p =>
                p.StartDate.Date <= today &&
                p.EndDate.Date >= today);

            vm.ActiveDietPlans = dietPlans.Count(p =>
                p.StartDate.Date <= today &&
                p.EndDate.Date >= today);

            var workoutItems = workoutPlans
                .Select(p => new ClientTrainerPlanItem
                {
                    PlanId = p.WorkoutPlanId,
                    PlanType = "Workout",
                    PlanName = p.PlanName,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Status = GetPlanStatus(
                        p.StartDate,
                        p.EndDate,
                        today)
                });

            var dietItems = dietPlans
                .Select(p => new ClientTrainerPlanItem
                {
                    PlanId = p.DietPlanId,
                    PlanType = "Diet",
                    PlanName = p.PlanName,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Status = GetPlanStatus(
                        p.StartDate,
                        p.EndDate,
                        today)
                });

            vm.RecentPlans = workoutItems
                .Concat(dietItems)
                .OrderByDescending(p => p.StartDate)
                .Take(6)
                .ToList();

            var appointments = db.Set<Appointment>()
                .Where(a =>
                    a.ClientId == clientId &&
                    a.TrainerId == trainerId &&
                    a.AppointmentDateTime >= now &&
                    (a.Status == "Scheduled" ||
                     a.Status == "Approved"))
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => new
                {
                    a.AppointmentId,
                    a.AppointmentDateTime,
                    a.Status,
                    a.Location
                })
                .ToList();

            vm.UpcomingAppointments = appointments.Count;

            vm.UpcomingSessions = appointments
                .Take(5)
                .Select(a => new ClientTrainerAppointmentItem
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentDateTime = a.AppointmentDateTime,
                    Status = a.Status,
                    Location = a.Location
                })
                .ToList();

            return View(vm);
        }

        private static string GetPlanStatus(
            DateTime startDate,
            DateTime endDate,
            DateTime today)
        {
            if (startDate.Date > today)
            {
                return "Upcoming";
            }

            if (endDate.Date < today)
            {
                return "Ended";
            }

            return "Active";
        }
    }
}
