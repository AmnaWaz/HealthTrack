using System;
using System.Linq;
using System.Data;
using System.Data.Entity;
using HealthTrack.Services;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class AppointmentsController : TrainerBaseController
    {
        private const string FormView = "Form";

        // GET: /Appointments?filter=upcoming|needsupdate|completed|cancelled|all
        public ActionResult Index(string filter)
        {
            filter = string.IsNullOrEmpty(filter) ? "upcoming" : filter.ToLower();
            var tid = CurrentTrainerId;
            var now = DateTime.Now;

            var mine = db.Set<Appointment>().Where(a => a.TrainerId == tid);

            var vm = new AppointmentsIndexViewModel { Filter = filter };
            vm.CountPending = mine.Count(a => a.Status == AppointmentStatus.Pending && a.AppointmentDateTime >= now);
            vm.CountUpcoming = mine.Count(a => (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Approved) && a.AppointmentDateTime >= now);
            vm.CountNeedsUpdate = mine.Count(a => (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Approved) && a.AppointmentDateTime < now);
            vm.CountCompleted = mine.Count(a => a.Status == "Completed");
            vm.CountCancelled = mine.Count(a => a.Status == "Cancelled" || a.Status == "Declined");
            vm.CountAll = mine.Count();

            IQueryable<Appointment> q = mine;
            if (filter == "pending") q = q.Where(a => a.Status == AppointmentStatus.Pending && a.AppointmentDateTime >= now);
            else if (filter == "upcoming") q = q.Where(a => (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Approved) && a.AppointmentDateTime >= now);
            else if (filter == "needsupdate") q = q.Where(a => (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Approved) && a.AppointmentDateTime < now);
            else if (filter == "completed") q = q.Where(a => a.Status == "Completed");
            else if (filter == "cancelled") q = q.Where(a => a.Status == "Cancelled" || a.Status == "Declined");
            else filter = "all";
            vm.Filter = filter;

            var raw = (from a in q
                       join c in db.Set<ClientProfile>() on a.ClientId equals c.ClientId
                       join u in db.Set<ApplicationUser>() on c.UserId equals u.UserId
                       select new { a.AppointmentId, When = a.AppointmentDateTime, u.FirstName, u.LastName, a.Status, a.Location })
                      .ToList();

            var rows = raw.Select(r => new AppointmentRow
            {
                AppointmentId = r.AppointmentId,
                When = r.When,
                ClientName = r.FirstName + " " + r.LastName,
                Status = r.Status,
                Location = r.Location
            });

            vm.Items = (filter == "upcoming" ? rows.OrderBy(r => r.When) : rows.OrderByDescending(r => r.When)).ToList();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            using (var booking = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
            var existing = db.Set<Appointment>().AsNoTracking().FirstOrDefault(a => a.AppointmentId == id && a.TrainerId == CurrentTrainerId);
            if (existing == null) return HttpNotFound();
            AppointmentBookingGuard.Lock(db, existing.ClientId, CurrentTrainerId);
            var appt = db.Set<Appointment>().FirstOrDefault(a => a.AppointmentId == id && a.TrainerId == CurrentTrainerId);
            if (appt == null) return HttpNotFound();
            if (appt.Status != AppointmentStatus.Pending || appt.AppointmentDateTime <= DateTime.Now || !IsMyClient(appt.ClientId))
            {
                TempData["Error"] = "This request is no longer eligible for approval.";
                return RedirectToAction("Index", new { filter = "pending" });
            }
            var start = appt.AppointmentDateTime.AddMinutes(-30);
            var end = appt.AppointmentDateTime.AddMinutes(30);
            var tid = CurrentTrainerId;
            if (db.Set<Appointment>().Any(a => (a.TrainerId == tid || a.ClientId == appt.ClientId) && a.AppointmentId != id &&
                (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Approved) &&
                a.AppointmentDateTime > start && a.AppointmentDateTime < end))
            {
                TempData["Error"] = "Another confirmed appointment conflicts with this request.";
                return RedirectToAction("Index", new { filter = "pending" });
            }
            appt.Status = AppointmentStatus.Scheduled;
            Notify(appt.ClientId, TrainerName + " approved your appointment for " + Stamp(appt.AppointmentDateTime) + ".", "Appointment");
            db.SaveChanges();
            booking.Commit();
            TempData["Success"] = "Appointment approved. The client has been notified.";
            return RedirectToAction("Index", new { filter = "pending" });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Decline(int id)
        {
            var appt = db.Set<Appointment>().FirstOrDefault(a => a.AppointmentId == id && a.TrainerId == CurrentTrainerId);
            if (appt == null) return HttpNotFound();
            if (appt.Status != AppointmentStatus.Pending)
            {
                TempData["Error"] = "Only pending requests can be declined.";
                return RedirectToAction("Index", new { filter = "pending" });
            }
            appt.Status = "Declined";
            Notify(appt.ClientId, TrainerName + " declined your appointment request for " + Stamp(appt.AppointmentDateTime) + ".", "Appointment");
            db.SaveChanges();
            TempData["Success"] = "Request declined. The client has been notified.";
            return RedirectToAction("Index", new { filter = "pending" });
        }

        // GET: /Appointments/Create?clientId=5
        public ActionResult Create(int? clientId)
        {
            var vm = Prepared(new AppointmentFormViewModel
            {
                ClientId = clientId,
                When = DateTime.Today.AddDays(1).AddHours(9)
            });
            return View(FormView, vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(AppointmentFormViewModel vm)
        {
            Prepared(vm);
            if (!Validate(vm, 0)) return View(FormView, vm);

            using (var booking = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
            AppointmentBookingGuard.Lock(db, vm.ClientId.Value, CurrentTrainerId);
            if (!Validate(vm, 0)) return View(FormView, vm);
            db.Set<Appointment>().Add(new Appointment
            {
                AppointmentDateTime = vm.When.Value,
                Status = AppointmentStatus.Scheduled,
                Location = vm.Location,
                Notes = vm.Notes,
                ClientId = vm.ClientId.Value,
                TrainerId = CurrentTrainerId
            });

            Notify(vm.ClientId.Value,
                TrainerName + " scheduled an appointment for " + Stamp(vm.When.Value) + AtPlace(vm.Location) + ".",
                "Appointment");
            db.SaveChanges();
            booking.Commit();
            }

            TempData["Success"] = "Appointment scheduled.";
            return RedirectToAction("Index");
        }

        // GET: /Appointments/Edit/5   (reschedule)
        public ActionResult Edit(int id)
        {
            var appt = db.Set<Appointment>().Find(id);
            if (appt == null || appt.TrainerId != CurrentTrainerId) return HttpNotFound();
            if (!IsMyClient(appt.ClientId))
            {
                TempData["Error"] = "This appointment belongs to a former client and cannot be changed.";
                return RedirectToAction("Index", new { filter = "all" });
            }
            if ((appt.Status != AppointmentStatus.Scheduled && appt.Status != AppointmentStatus.Approved))
            {
                TempData["Error"] = "Only scheduled appointments can be changed.";
                return RedirectToAction("Index", new { filter = "all" });
            }

            var vm = Prepared(new AppointmentFormViewModel
            {
                Id = id,
                ClientId = appt.ClientId,
                When = appt.AppointmentDateTime,
                Location = appt.Location,
                Notes = appt.Notes
            });
            return View(FormView, vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(AppointmentFormViewModel vm)
        {
            var appt = db.Set<Appointment>().Find(vm.Id);
            if (appt == null || appt.TrainerId != CurrentTrainerId) return HttpNotFound();
            if (!IsMyClient(appt.ClientId))
            {
                TempData["Error"] = "This appointment belongs to a former client and cannot be changed.";
                return RedirectToAction("Index", new { filter = "all" });
            }

            Prepared(vm);
            if (vm.ClientId != appt.ClientId) ModelState.AddModelError("ClientId", "You cannot change the client for an existing appointment.");
            if ((appt.Status != AppointmentStatus.Scheduled && appt.Status != AppointmentStatus.Approved)) ModelState.AddModelError("", "Only scheduled appointments can be edited.");
            if (!Validate(vm, vm.Id)) return View(FormView, vm);

            using (var booking = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
            AppointmentBookingGuard.Lock(db, appt.ClientId, CurrentTrainerId);
            db.Entry(appt).Reload();
            if (!IsMyClient(appt.ClientId) ||
                (appt.Status != AppointmentStatus.Scheduled && appt.Status != AppointmentStatus.Approved) ||
                vm.ClientId != appt.ClientId || !Validate(vm, vm.Id))
            {
                TempData["Error"] = "This appointment is no longer eligible for editing.";
                return RedirectToAction("Index", new { filter = "all" });
            }
            var timeChanged = appt.AppointmentDateTime != vm.When.Value;

            appt.AppointmentDateTime = vm.When.Value;
            appt.Location = vm.Location;
            appt.Notes = vm.Notes;
            appt.ClientId = vm.ClientId.Value;

            if (timeChanged)
                Notify(vm.ClientId.Value,
                    TrainerName + " moved your appointment to " + Stamp(vm.When.Value) + AtPlace(vm.Location) + ".",
                    "Appointment");
            db.SaveChanges();
            booking.Commit();
            }

            TempData["Success"] = "Appointment updated.";
            return RedirectToAction("Index");
        }

        // POST: /Appointments/Complete/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Complete(int id, string filter)
        {
            var appt = db.Set<Appointment>().Find(id);
            if (appt == null || appt.TrainerId != CurrentTrainerId) return HttpNotFound();
            if (!IsMyClient(appt.ClientId))
            {
                TempData["Error"] = "This appointment belongs to a former client and cannot be changed.";
                return RedirectToAction("Index", new { filter = "all" });
            }

            if ((appt.Status == AppointmentStatus.Scheduled || appt.Status == AppointmentStatus.Approved))
            {
                appt.Status = "Completed";
                db.SaveChanges();
                TempData["Success"] = "Appointment marked as completed.";
            }
            return RedirectToAction("Index", new { filter = filter });
        }

        // POST: /Appointments/Cancel/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Cancel(int id, string filter)
        {
            var appt = db.Set<Appointment>().Find(id);
            if (appt == null || appt.TrainerId != CurrentTrainerId) return HttpNotFound();
            if (!IsMyClient(appt.ClientId))
            {
                TempData["Error"] = "This appointment belongs to a former client and cannot be changed.";
                return RedirectToAction("Index", new { filter = "all" });
            }

            if ((appt.Status == AppointmentStatus.Scheduled || appt.Status == AppointmentStatus.Approved))
            {
                appt.Status = "Cancelled";
                Notify(appt.ClientId,
                    TrainerName + " cancelled your appointment on " + Stamp(appt.AppointmentDateTime) + ".",
                    "Appointment");
                db.SaveChanges();
                TempData["Success"] = "Appointment cancelled.";
            }
            return RedirectToAction("Index", new { filter = filter });
        }

        // ---------- helpers ----------
        private AppointmentFormViewModel Prepared(AppointmentFormViewModel vm)
        {
            vm.Clients = MyClientOptions();
            return vm;
        }

        private bool Validate(AppointmentFormViewModel vm, int excludeId)
        {
            if (vm.When.HasValue && vm.When.Value <= DateTime.Now)
                ModelState.AddModelError("When", "Choose a date and time in the future.");

            if (vm.ClientId.HasValue && !IsMyClient(vm.ClientId.Value))
                ModelState.AddModelError("ClientId", "You can only book appointments with your own clients.");

            if (vm.When.HasValue)
            {
                var tid = CurrentTrainerId;
                var windowStart = vm.When.Value.AddMinutes(-30);
                var windowEnd = vm.When.Value.AddMinutes(30);
                var clash = db.Set<Appointment>().Any(a =>
                    (a.TrainerId == tid || a.ClientId == vm.ClientId.Value) && (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Approved) && a.AppointmentId != excludeId &&
                    a.AppointmentDateTime > windowStart && a.AppointmentDateTime < windowEnd);
                if (clash)
                    ModelState.AddModelError("When", "You already have an appointment within 30 minutes of that time.");
            }
            return ModelState.IsValid;
        }

        private static string Stamp(DateTime d)
        {
            return d.ToString("ddd d MMM, h:mm tt");
        }

        private static string AtPlace(string location)
        {
            return string.IsNullOrWhiteSpace(location) ? "" : " at " + location.Trim();
        }
    }
}
