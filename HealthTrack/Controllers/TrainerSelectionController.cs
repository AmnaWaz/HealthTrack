
using System;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class TrainerSelectionController : TrainerBaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View(BuildViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Accept(TrainerSelectionDecisionForm form)
        {
            if (form == null || !ModelState.IsValid)
            {
                TempData["Error"] = "Invalid trainer selection request.";
                return RedirectToAction("Index");
            }

            var trainerId = CurrentTrainerId;

            using (var transaction = db.Database.BeginTransaction(
                IsolationLevel.Serializable))
            {
                var request = db.Set<TrainerSelectionRequest>()
                    .FirstOrDefault(r =>
                        r.TrainerSelectionRequestId ==
                            form.TrainerSelectionRequestId &&
                        r.TrainerId == trainerId);

                if (request == null)
                {
                    return HttpNotFound();
                }

                if (request.Status !=
                    TrainerSelectionRequest.Statuses.Pending)
                {
                    TempData["Error"] =
                        "This selection request is no longer pending.";

                    return RedirectToAction("Index");
                }

                var client = db.Set<ClientProfile>()
                    .FirstOrDefault(c =>
                        c.ClientId == request.ClientId);

                if (client == null)
                {
                    return HttpNotFound();
                }

                var clientUser = db.Set<ApplicationUser>()
                    .FirstOrDefault(u =>
                        u.UserId == client.UserId);

                if (clientUser == null || !clientUser.IsActive)
                {
                    TempData["Error"] =
                        "This client account is not active.";

                    return RedirectToAction("Index");
                }

                if (client.TrainerId.HasValue)
                {
                    TempData["Error"] =
                        "This client already has an assigned trainer.";

                    return RedirectToAction("Index");
                }

                var now = DateTime.Now;

                client.TrainerId = trainerId;

                request.Status =
                    TrainerSelectionRequest.Statuses.Accepted;

                request.RespondedAt = now;

                request.TrainerNote =
                    string.IsNullOrWhiteSpace(form.TrainerNote)
                        ? null
                        : form.TrainerNote.Trim();

                var otherPendingRequests = db.Set<TrainerSelectionRequest>()
                    .Where(r => r.ClientId == client.ClientId &&
                                r.TrainerSelectionRequestId != request.TrainerSelectionRequestId &&
                                r.Status == TrainerSelectionRequest.Statuses.Pending)
                    .ToList();

                foreach (var other in otherPendingRequests)
                {
                    other.Status = TrainerSelectionRequest.Statuses.Cancelled;
                    other.RespondedAt = now;
                }

                // Legacy data may contain future bookings from an older relationship,
                // even though this Client currently has no assigned Trainer.
                var staleAppointments = db.Set<Appointment>()
                    .Where(a => a.ClientId == client.ClientId &&
                                a.TrainerId != trainerId &&
                                a.AppointmentDateTime > now &&
                                (a.Status == AppointmentStatus.Pending ||
                                 a.Status == AppointmentStatus.Scheduled ||
                                 a.Status == AppointmentStatus.Approved))
                    .ToList();

                foreach (var appointment in staleAppointments)
                {
                    appointment.Status = AppointmentStatus.Cancelled;
                }

                var otherTrainerIds = otherPendingRequests
                    .Select(r => r.TrainerId)
                    .Concat(staleAppointments.Select(a => a.TrainerId))
                    .Distinct()
                    .ToList();

                var affectedTrainers = db.Set<TrainerProfile>()
                    .Where(t => otherTrainerIds.Contains(t.TrainerId))
                    .Select(t => new { t.TrainerId, t.UserId })
                    .ToList();

                var clientName = (clientUser.FirstName + " " + clientUser.LastName).Trim();
                foreach (var affectedTrainer in affectedTrainers)
                {
                    var hadSelection = otherPendingRequests.Any(r => r.TrainerId == affectedTrainer.TrainerId);
                    var hadAppointment = staleAppointments.Any(a => a.TrainerId == affectedTrainer.TrainerId);
                    var message = hadSelection && hadAppointment
                        ? "The trainer request from " + clientName + " was closed, and old upcoming appointments were cancelled because the client selected another trainer."
                        : hadSelection
                            ? "The trainer request from " + clientName + " was closed because the client selected another trainer."
                            : "Upcoming appointments for " + clientName + " were cancelled because the client selected another trainer.";
                    db.Set<Notification>().Add(new Notification
                    {
                        UserId = affectedTrainer.UserId,
                        Message = LimitNotification(message),
                        Type = "TrainerSelection",
                        IsRead = false,
                        CreatedAt = now
                    });
                }

                db.Set<Notification>().Add(new Notification
                {
                    UserId = client.UserId,
                    Message = LimitNotification(
                        "Your trainer selection request was accepted by " +
                        TrainerName + ". You can now access your trainer's plans and services." +
                        (staleAppointments.Count > 0
                            ? " Old appointments with other trainers were cancelled."
                            : "")),
                    Type = "TrainerSelection",
                    IsRead = false,
                    CreatedAt = now
                });

                db.SaveChanges();

                transaction.Commit();
            }

            TempData["Success"] =
                "Trainer selection accepted. The client is now assigned to you.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(TrainerSelectionDecisionForm form)
        {
            if (form == null || !ModelState.IsValid)
            {
                TempData["Error"] = "Invalid trainer selection request.";
                return RedirectToAction("Index");
            }

            var trainerId = CurrentTrainerId;

            using (var transaction = db.Database.BeginTransaction(
                IsolationLevel.Serializable))
            {
                var request = db.Set<TrainerSelectionRequest>()
                    .FirstOrDefault(r =>
                        r.TrainerSelectionRequestId ==
                            form.TrainerSelectionRequestId &&
                        r.TrainerId == trainerId);

                if (request == null)
                {
                    return HttpNotFound();
                }

                if (request.Status !=
                    TrainerSelectionRequest.Statuses.Pending)
                {
                    TempData["Error"] =
                        "This selection request is no longer pending.";

                    return RedirectToAction("Index");
                }

                var now = DateTime.Now;

                request.Status =
                    TrainerSelectionRequest.Statuses.Rejected;

                request.RespondedAt = now;

                request.TrainerNote =
                    string.IsNullOrWhiteSpace(form.TrainerNote)
                        ? null
                        : form.TrainerNote.Trim();

                var client = db.Set<ClientProfile>()
                    .FirstOrDefault(c =>
                        c.ClientId == request.ClientId);

                if (client != null)
                {
                    db.Set<Notification>().Add(new Notification
                    {
                        UserId = client.UserId,
                        Message = LimitNotification(
                            "Your trainer selection request to " +
                            TrainerName +
                            " was not accepted. You can browse other available trainers."),
                        Type = "TrainerSelection",
                        IsRead = false,
                        CreatedAt = now
                    });
                }

                db.SaveChanges();

                transaction.Commit();
            }

            TempData["Success"] =
                "Trainer selection request rejected.";

            return RedirectToAction("Index");
        }

        private static string LimitNotification(string message)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;
            return message.Length <= 300 ? message : message.Substring(0, 300);
        }

        private TrainerSelectionViewModel BuildViewModel()
        {
            var trainerId = CurrentTrainerId;

            var requests = (
                from r in db.Set<TrainerSelectionRequest>()
                join c in db.Set<ClientProfile>()
                    on r.ClientId equals c.ClientId
                join u in db.Set<ApplicationUser>()
                    on c.UserId equals u.UserId
                where r.TrainerId == trainerId
                orderby r.CreatedAt descending,
                        r.TrainerSelectionRequestId descending
                select new TrainerSelectionItem
                {
                    TrainerSelectionRequestId =
                        r.TrainerSelectionRequestId,
                    ClientId = r.ClientId,
                    ClientName = u.FirstName + " " + u.LastName,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    RespondedAt = r.RespondedAt,
                    TrainerNote = r.TrainerNote
                }
            ).ToList();

            var clientIds = requests
                .Select(r => r.ClientId)
                .Distinct()
                .ToList();

            var goals = (
                from cg in db.Set<ClientGoal>()
                join fg in db.Set<FitnessGoal>()
                    on cg.FitnessGoalId equals fg.FitnessGoalId
                where clientIds.Contains(cg.ClientId)
                orderby cg.Position
                select new
                {
                    cg.ClientId,
                    fg.Name
                }
            ).ToList();

            foreach (var request in requests)
            {
                request.FitnessGoals = goals
                    .Where(g => g.ClientId == request.ClientId)
                    .Select(g => g.Name)
                    .ToList();
            }

            return new TrainerSelectionViewModel
            {
                TrainerName = TrainerName,

                PendingCount = requests.Count(r =>
                    r.Status ==
                    TrainerSelectionRequest.Statuses.Pending),

                AcceptedCount = requests.Count(r =>
                    r.Status ==
                    TrainerSelectionRequest.Statuses.Accepted),

                RejectedCount = requests.Count(r =>
                    r.Status ==
                    TrainerSelectionRequest.Statuses.Rejected),

                CancelledCount = requests.Count(r =>
                    r.Status ==
                    TrainerSelectionRequest.Statuses.Cancelled),

                Requests = requests
                    .OrderByDescending(r =>
                        r.Status ==
                        TrainerSelectionRequest.Statuses.Pending)
                    .ThenByDescending(r => r.CreatedAt)
                    .ToList()
            };
        }
    }
}
