using System;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class TrainerController : TrainerBaseController
    {
        // GET: /Trainer  (dashboard)
        public ActionResult Index()
        {
            var tid = CurrentTrainerId;
            var userId = CurrentUserId;
            var now = DateTime.Now;
            var today = now.Date;

            var vm = new TrainerDashboardViewModel
            {
                Greeting = now.Hour < 12 ? "Good morning" : (now.Hour < 18 ? "Good afternoon" : "Good evening"),
                FirstName = CurrentUser.FirstName,
                Specialisation = CurrentTrainer.Specialisation,
                AverageRating = (decimal)CurrentTrainer.AverageRating
            };

            vm.ClientCount = db.Set<ClientProfile>().Count(c => c.TrainerId == tid);

            vm.UpcomingAppointmentCount = db.Set<Appointment>()
                .Count(a => a.TrainerId == tid && a.AppointmentDateTime >= now && (a.Status == "Scheduled" || a.Status == "Approved"));

            vm.ActiveWorkoutPlans = db.Set<WorkoutPlan>()
                .Count(p => p.TrainerId == tid && p.StartDate <= today && (p.EndDate == null || p.EndDate >= today));

            vm.ActiveDietPlans = db.Set<DietPlan>()
                .Count(p => p.TrainerId == tid && p.StartDate <= today && (p.EndDate == null || p.EndDate >= today));

            vm.UnreadNotifications = db.Set<Notification>().Count(n => n.UserId == userId && !n.IsRead);

            var appts = (from a in db.Set<Appointment>()
                         join c in db.Set<ClientProfile>() on a.ClientId equals c.ClientId
                         join u in db.Set<ApplicationUser>() on c.UserId equals u.UserId
                         where a.TrainerId == tid && a.AppointmentDateTime >= now && (a.Status == "Scheduled" || a.Status == "Approved")
                         orderby a.AppointmentDateTime
                         select new { a.AppointmentId, When = a.AppointmentDateTime, u.FirstName, u.LastName, a.Status, a.Location })
                        .Take(6).ToList();

            vm.Appointments = appts.Select(a => new AppointmentRow
            {
                AppointmentId = a.AppointmentId,
                When = a.When,
                ClientName = a.FirstName + " " + a.LastName,
                Status = a.Status,
                Location = a.Location
            }).ToList();

            vm.Clients = ClientRows(true);

            vm.Notifications = db.Set<Notification>()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToList()
                .Select(n => new NotificationRow { Message = n.Message, Type = n.Type, CreatedAt = n.CreatedAt, IsRead = n.IsRead })
                .ToList();

            return View(vm);
        }

        // GET: /Trainer/Clients  (my clients + clients who have no trainer yet)
        public ActionResult Clients()
        {
            return View(new TrainerClientsViewModel
            {
                Mine = ClientRows(true),
                Available = new System.Collections.Generic.List<ClientRow>()
            });
        }

    }
}
