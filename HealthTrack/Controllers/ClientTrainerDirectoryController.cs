
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class ClientTrainerDirectoryController : ClientBaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View(BuildViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestTrainer(int trainerId)
        {
            var clientId = CurrentClientId;

            using (var transaction = db.Database.BeginTransaction(
                IsolationLevel.Serializable))
            {
                var client = db.Set<ClientProfile>()
                    .FirstOrDefault(c => c.ClientId == clientId);

                if (client == null)
                {
                    return HttpNotFound();
                }

                if (client.TrainerId.HasValue)
                {
                    TempData["Error"] =
                        "You already have an assigned trainer.";

                    return RedirectToAction("Index");
                }

                var hasPending = db.Set<TrainerSelectionRequest>()
                    .Any(r =>
                        r.ClientId == clientId &&
                        r.Status == TrainerSelectionRequest.Statuses.Pending);

                if (hasPending)
                {
                    TempData["Error"] =
                        "You already have a pending trainer request.";

                    return RedirectToAction("Index");
                }

                var trainer = (
                    from t in db.Set<TrainerProfile>()
                    join u in db.Set<ApplicationUser>()
                        on t.UserId equals u.UserId
                    join role in db.Set<Role>()
                        on u.RoleId equals role.RoleId
                    where t.TrainerId == trainerId
                          && u.IsActive
                          && role.RoleName == Role.Trainer
                    select new
                    {
                        t.TrainerId,
                        u.FirstName,
                        u.LastName,
                        u.UserId
                    }
                ).FirstOrDefault();

                if (trainer == null)
                {
                    TempData["Error"] =
                        "This trainer is no longer available.";

                    return RedirectToAction("Index");
                }

                var now = DateTime.Now;

                var request = new TrainerSelectionRequest
                {
                    ClientId = clientId,
                    TrainerId = trainerId,
                    Status = TrainerSelectionRequest.Statuses.Pending,
                    CreatedAt = now
                };

                db.Set<TrainerSelectionRequest>().Add(request);
                var clientName = (CurrentUser.FirstName + " " + CurrentUser.LastName).Trim();
                var message = "New trainer connection request from " + clientName + ". Review it under Client Selections.";
                if (message.Length > 300) message = message.Substring(0, 300);
                db.Set<Notification>().Add(new Notification
                {
                    UserId = trainer.UserId,
                    Message = message,
                    Type = "TrainerSelection",
                    IsRead = false,
                    CreatedAt = now
                });
                db.SaveChanges();

                transaction.Commit();

                TempData["Success"] =
                    "Your trainer selection request has been sent.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelRequest(int id)
        {
            var clientId = CurrentClientId;

            using (var transaction = db.Database.BeginTransaction(
                IsolationLevel.Serializable))
            {
                var request = db.Set<TrainerSelectionRequest>()
                    .FirstOrDefault(r =>
                        r.TrainerSelectionRequestId == id &&
                        r.ClientId == clientId);

                if (request == null)
                {
                    return HttpNotFound();
                }

                if (request.Status != TrainerSelectionRequest.Statuses.Pending)
                {
                    TempData["Error"] =
                        "Only pending trainer requests can be cancelled.";
                    return RedirectToAction("Index");
                }

                var trainerUserId = db.Set<TrainerProfile>()
                    .Where(t => t.TrainerId == request.TrainerId)
                    .Select(t => (int?)t.UserId)
                    .FirstOrDefault();

                var now = DateTime.Now;
                request.Status = TrainerSelectionRequest.Statuses.Cancelled;
                request.RespondedAt = now;

                if (trainerUserId.HasValue)
                {
                    var clientName = ((CurrentUser.FirstName ?? "") + " " +
                        (CurrentUser.LastName ?? "")).Trim();
                    var message = "Trainer connection request cancelled by " +
                        clientName + ". Open Client Selections for the updated status.";
                    if (message.Length > 300)
                    {
                        message = message.Substring(0, 300);
                    }

                    db.Set<Notification>().Add(new Notification
                    {
                        UserId = trainerUserId.Value,
                        Message = message,
                        Type = "TrainerSelection",
                        IsRead = false,
                        CreatedAt = now
                    });
                }

                db.SaveChanges();
                transaction.Commit();
            }

            TempData["Success"] =
                "Your pending trainer request has been cancelled.";

            return RedirectToAction("Index");
        }

        private ClientTrainerDirectoryViewModel BuildViewModel()
        {
            var clientId = CurrentClientId;

            var vm = new ClientTrainerDirectoryViewModel
            {
                HasAssignedTrainer = CurrentClient.TrainerId.HasValue
            };

            if (CurrentClient.TrainerId.HasValue)
            {
                var assignedId = CurrentClient.TrainerId.Value;

                vm.AssignedTrainerName = (
                    from t in db.Set<TrainerProfile>()
                    join u in db.Set<ApplicationUser>()
                        on t.UserId equals u.UserId
                    where t.TrainerId == assignedId
                    select u.FirstName + " " + u.LastName
                ).FirstOrDefault();
            }

            var pending = db.Set<TrainerSelectionRequest>()
                .Where(r =>
                    r.ClientId == clientId &&
                    r.Status == TrainerSelectionRequest.Statuses.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            if (pending != null)
            {
                vm.HasPendingRequest = true;
                vm.PendingTrainerId = pending.TrainerId;

                vm.PendingTrainerName = (
                    from t in db.Set<TrainerProfile>()
                    join u in db.Set<ApplicationUser>()
                        on t.UserId equals u.UserId
                    where t.TrainerId == pending.TrainerId
                    select u.FirstName + " " + u.LastName
                ).FirstOrDefault();
            }

            var trainers = (
                from t in db.Set<TrainerProfile>()
                join u in db.Set<ApplicationUser>()
                    on t.UserId equals u.UserId
                join role in db.Set<Role>()
                    on u.RoleId equals role.RoleId
                where u.IsActive &&
                      role.RoleName == Role.Trainer
                orderby u.FirstName, u.LastName
                select new
                {
                    t.TrainerId,
                    t.Bio,
                    u.FirstName,
                    u.LastName,
                    u.ProfilePhotoPath
                }
            ).ToList();

            var trainerIds = trainers
                .Select(t => t.TrainerId)
                .ToList();

            var specialisations = (
                from ts in db.Set<TrainerSpecialisation>()
                join s in db.Set<Specialisation>()
                    on ts.SpecialisationId equals s.SpecialisationId
                where trainerIds.Contains(ts.TrainerId)
                orderby ts.Position, s.SortOrder
                select new
                {
                    ts.TrainerId,
                    s.Name
                }
            ).ToList();

            vm.Trainers = trainers
                .Select(t => new ClientTrainerDirectoryItem
                {
                    TrainerId = t.TrainerId,
                    FullName = (
                        (t.FirstName ?? "") + " " +
                        (t.LastName ?? "")
                    ).Trim(),
                    Bio = t.Bio,
                    ProfilePhotoPath = t.ProfilePhotoPath,
                    Specialisations = specialisations
                        .Where(s => s.TrainerId == t.TrainerId)
                        .Select(s => s.Name)
                        .Distinct()
                        .ToList(),
                    IsAssigned =
                        CurrentClient.TrainerId == t.TrainerId,
                    IsPending =
                        vm.PendingTrainerId == t.TrainerId,
                    CanRequest =
                        !vm.HasAssignedTrainer &&
                        !vm.HasPendingRequest
                })
                .ToList();

            vm.RequestHistory = (
                from r in db.Set<TrainerSelectionRequest>()
                join t in db.Set<TrainerProfile>()
                    on r.TrainerId equals t.TrainerId
                join u in db.Set<ApplicationUser>()
                    on t.UserId equals u.UserId
                where r.ClientId == clientId
                orderby r.CreatedAt descending,
                        r.TrainerSelectionRequestId descending
                select new ClientTrainerSelectionHistoryItem
                {
                    TrainerSelectionRequestId =
                        r.TrainerSelectionRequestId,
                    TrainerId = r.TrainerId,
                    TrainerName = u.FirstName + " " + u.LastName,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    RespondedAt = r.RespondedAt,
                    TrainerNote = r.TrainerNote
                }
            ).ToList();

            return vm;
        }
    }
}
