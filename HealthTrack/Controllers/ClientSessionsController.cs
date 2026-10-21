
using System;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class ClientSessionsController : ClientBaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            var clientId = CurrentClientId;
            var now = DateTime.Now;

            var vm = new ClientSessionsViewModel
            {
                FirstName = CurrentUser.FirstName,
                HasAssignedTrainer = CurrentClient.TrainerId.HasValue
            };

            if (CurrentClient.TrainerId.HasValue)
            {
                var trainerId = CurrentClient.TrainerId.Value;

                vm.TrainerName = (
                    from t in db.Set<TrainerProfile>()
                    join u in db.Set<ApplicationUser>()
                        on t.UserId equals u.UserId
                    where t.TrainerId == trainerId
                    select u.FirstName + " " + u.LastName
                ).FirstOrDefault();
            }

            var appointments = (
                from a in db.Set<Appointment>()
                join t in db.Set<TrainerProfile>()
                    on a.TrainerId equals t.TrainerId
                join u in db.Set<ApplicationUser>()
                    on t.UserId equals u.UserId
                where a.ClientId == clientId
                select new
                {
                    a.AppointmentId,
                    a.TrainerId,
                    a.AppointmentDateTime,
                    a.Status,
                    a.Location,
                    a.Notes,
                    u.FirstName,
                    u.LastName
                }
            ).ToList();

            var items = appointments
                .Select(a => new ClientAppointmentItem
                {
                    AppointmentId = a.AppointmentId,
                    TrainerId = a.TrainerId,
                    TrainerName = a.FirstName + " " + a.LastName,
                    AppointmentDateTime = a.AppointmentDateTime,
                    Status = a.Status,
                    Location = a.Location,
                    Notes = a.Notes,
                    CanWithdraw = a.Status == "Pending" &&
                                  a.AppointmentDateTime > now,
                    IsUpcoming = a.AppointmentDateTime >= now
                })
                .ToList();

            vm.PendingAppointments = items
                .Where(a => a.Status == "Pending" &&
                            a.AppointmentDateTime >= now)
                .OrderBy(a => a.AppointmentDateTime)
                .ToList();

            vm.UpcomingAppointments = items
                .Where(a =>
                    (a.Status == "Scheduled" ||
                     a.Status == "Approved") &&
                    a.AppointmentDateTime >= now)
                .OrderBy(a => a.AppointmentDateTime)
                .ToList();

            vm.AppointmentHistory = items
                .Where(a =>
                    a.Status == "Completed" ||
                    a.Status == "Cancelled" ||
                    a.Status == "Declined" ||
                    a.Status == "Rejected" ||
                    a.AppointmentDateTime < now)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToList();

            vm.PendingCount = vm.PendingAppointments.Count;
            vm.UpcomingCount = vm.UpcomingAppointments.Count;

            vm.CompletedCount = items.Count(a =>
                a.Status == "Completed");

            vm.CancelledCount = items.Count(a =>
                a.Status == "Cancelled" ||
                a.Status == "Declined" ||
                a.Status == "Rejected");

            vm.RequestForm = new ClientAppointmentRequestForm
            {
                Date = DateTime.Today.AddDays(1),
                Time = new TimeSpan(9, 0, 0)
            };

            return View(vm);
        }
    }
}
