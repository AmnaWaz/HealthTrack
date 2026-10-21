
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

                db.Set<Notification>().Add(new Notification
                {
                    UserId = client.UserId,
                    Message =
                        "Your trainer selection request was accepted by " +
                        TrainerName + ". You can now access your trainer's plans and services.",
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
                        Message =
                            "Your trainer selection request to " +
                            TrainerName +
                            " was not accepted. You can browse other available trainers.",
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
