
using System;
using System.Linq;
using System.Data;
using HealthTrack.Services;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestAppointment(ClientAppointmentRequestForm form)
        {
            if (!CurrentClient.TrainerId.HasValue)
            {
                TempData["Error"] = "Choose a trainer before requesting a session.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid || !form.Date.HasValue || !form.Time.HasValue)
            {
                TempData["Error"] = "Enter a valid date, time and notes (up to 500 characters).";
                return RedirectToAction("Index");
            }

            var when = form.Date.Value.Date.Add(form.Time.Value);
            if (when <= DateTime.Now)
            {
                TempData["Error"] = "Choose an appointment date and time in the future.";
                return RedirectToAction("Index");
            }

            var trainerId = CurrentClient.TrainerId.Value;
            var trainer = (from t in db.Set<TrainerProfile>()
                           join u in db.Set<ApplicationUser>() on t.UserId equals u.UserId
                           where t.TrainerId == trainerId && u.IsActive
                           select new { u.UserId }).FirstOrDefault();
            if (trainer == null)
            {
                TempData["Error"] = "Your assigned trainer is currently unavailable.";
                return RedirectToAction("Index");
            }

            using (var booking = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
            AppointmentBookingGuard.Lock(db, CurrentClientId, trainerId);
            // Recheck assignment and account status after obtaining the booking locks.
            if (db.Set<ClientProfile>().Where(c => c.ClientId == CurrentClientId)
                    .Select(c => c.TrainerId).FirstOrDefault() != trainerId ||
                !(from t in db.Set<TrainerProfile>()
                  join u in db.Set<ApplicationUser>() on t.UserId equals u.UserId
                  where t.TrainerId == trainerId && u.IsActive select t.TrainerId).Any())
            {
                TempData["Error"] = "Your trainer assignment has changed. Please reload the page.";
                return RedirectToAction("Index");
            }
            var windowStart = when.AddMinutes(-30);
            var windowEnd = when.AddMinutes(30);
            var conflict = db.Set<Appointment>().Any(a =>
                ((a.ClientId == CurrentClientId) || (a.TrainerId == trainerId &&
                    (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Approved))) &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Approved) &&
                a.AppointmentDateTime > windowStart && a.AppointmentDateTime < windowEnd);
            if (conflict)
            {
                TempData["Error"] = "An existing appointment or pending request conflicts with that time.";
                return RedirectToAction("Index");
            }

            db.Set<Appointment>().Add(new Appointment
            {
                ClientId = CurrentClientId,
                TrainerId = trainerId,
                AppointmentDateTime = when,
                Status = AppointmentStatus.Pending,
                Notes = (form.Notes ?? "").Trim()
            });
            var notification = "New appointment request from " + CurrentUser.FullName +
                               " for " + when.ToString("ddd d MMM, h:mm tt") + ".";
            if (notification.Length > 300) notification = notification.Substring(0, 300);
            db.Set<Notification>().Add(new Notification
            {
                UserId = trainer.UserId,
                Type = Notification.AppointmentReminder,
                Message = notification,
                IsRead = false,
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();
            booking.Commit();
            }
            TempData["Success"] = "Appointment request sent to your trainer.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Withdraw(int id)
        {
            var appointment = db.Set<Appointment>().FirstOrDefault(a =>
                a.AppointmentId == id && a.ClientId == CurrentClientId);
            if (appointment == null) return HttpNotFound();
            if (appointment.Status != AppointmentStatus.Pending || appointment.AppointmentDateTime <= DateTime.Now)
            {
                TempData["Error"] = "Only future pending appointment requests can be withdrawn.";
                return RedirectToAction("Index");
            }
            appointment.Status = AppointmentStatus.Cancelled;
            var trainerUserId = db.Set<TrainerProfile>()
                .Where(t => t.TrainerId == appointment.TrainerId)
                .Select(t => (int?)t.UserId).FirstOrDefault();
            if (trainerUserId.HasValue)
            {
                var message = CurrentUser.FullName + " withdrew the appointment request for " +
                              appointment.AppointmentDateTime.ToString("ddd d MMM, h:mm tt") + ".";
                if (message.Length > 300) message = message.Substring(0, 300);
                db.Set<Notification>().Add(new Notification
                {
                    UserId = trainerUserId.Value,
                    Message = message,
                    Type = Notification.AppointmentReminder,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            db.SaveChanges();
            TempData["Success"] = "Appointment request withdrawn.";
            return RedirectToAction("Index");
        }
    }
}
