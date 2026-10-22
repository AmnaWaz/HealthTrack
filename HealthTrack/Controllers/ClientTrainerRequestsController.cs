
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.Services;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class ClientTrainerRequestsController : ClientBaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View(BuildViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Submit(
            ClientTrainerRequestForm form,
            bool aiConsent = false)
        {
            if (form == null)
            {
                return new HttpStatusCodeResult(400);
            }

            var categories = new[]
            {
                TrainerRequest.Categories.MedicalConcern,
                TrainerRequest.Categories.WorkoutDifficulty,
                TrainerRequest.Categories.DietaryConcern,
                TrainerRequest.Categories.FitnessGoal,
                TrainerRequest.Categories.Availability,
                TrainerRequest.Categories.PlanAdjustment,
                TrainerRequest.Categories.GeneralQuestion
            };

            var priorities = new[]
            {
                TrainerRequest.Priorities.Normal,
                TrainerRequest.Priorities.High
            };

            if (!categories.Contains(form.Category))
            {
                ModelState.AddModelError(
                    "Category",
                    "Select a valid request category."
                );
            }

            if (!priorities.Contains(form.Priority))
            {
                ModelState.AddModelError(
                    "Priority",
                    "Select a valid priority."
                );
            }

            if (string.IsNullOrWhiteSpace(form.Subject) ||
                form.Subject.Trim().Length < 5 ||
                form.Subject.Trim().Length > 100)
            {
                ModelState.AddModelError(
                    "Subject",
                    "The subject must be between 5 and 100 characters."
                );
            }

            if (string.IsNullOrWhiteSpace(form.Description) ||
                form.Description.Trim().Length < 10 ||
                form.Description.Trim().Length > 1000)
            {
                ModelState.AddModelError(
                    "Description",
                    "The description must be between 10 and 1000 characters."
                );
            }

            if (!CurrentClient.TrainerId.HasValue)
            {
                ModelState.AddModelError(
                    "",
                    "You must have an assigned trainer before submitting a request."
                );
            }
            else
            {
                var trainerId = CurrentClient.TrainerId.Value;

                var trainerIsActive = (
                    from t in db.Set<TrainerProfile>()
                    join u in db.Set<ApplicationUser>()
                        on t.UserId equals u.UserId
                    where t.TrainerId == trainerId && u.IsActive
                    select t.TrainerId
                ).Any();

                if (!trainerIsActive)
                {
                    ModelState.AddModelError(
                        "",
                        "Your assigned trainer is currently unavailable."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                return View("Index", BuildViewModel(form));
            }

            var subject = form.Subject.Trim();
            var description = form.Description.Trim();

            TrainerRequestPriorityResult analysis;

            if (aiConsent)
            {
                analysis = await TrainerRequestAiService.AnalyzeAsync(
                    form.Category,
                    subject,
                    description,
                    form.Priority
                );
            }
            else
            {
                analysis = TrainerRequestPriorityAnalyzer.Analyze(
                    form.Category,
                    subject,
                    description,
                    form.Priority
                );
            }

            var effectivePriority =
                form.Priority == TrainerRequest.Priorities.High ||
                analysis.SuggestedPriority == TrainerRequest.Priorities.High
                    ? TrainerRequest.Priorities.High
                    : TrainerRequest.Priorities.Normal;

            var now = DateTime.Now;
            var trainerIdForRequest = CurrentClient.TrainerId.Value;

            var request = new TrainerRequest
            {
                ClientId = CurrentClientId,
                TrainerId = trainerIdForRequest,
                Category = form.Category,
                Priority = effectivePriority,
                Subject = subject,
                Description = description,
                Status = TrainerRequest.Statuses.Open,
                CreatedAt = now,
                UpdatedAt = now,
                AiSuggestedPriority = analysis.SuggestedPriority,
                AiReason = analysis.Reason,
                RequiresUrgentReview = analysis.RequiresUrgentReview,
                AiAnalysisSource = analysis.AnalysisSource
            };

            db.Set<TrainerRequest>().Add(request);

            var trainer = db.Set<TrainerProfile>()
                .FirstOrDefault(t =>
                    t.TrainerId == trainerIdForRequest);

            if (trainer != null)
            {
                var notificationMessage =
                    effectivePriority == TrainerRequest.Priorities.High
                        ? "High-priority trainer request received: " + subject
                        : "New trainer request received: " + subject;

                if (notificationMessage.Length > 300)
                {
                    notificationMessage =
                        notificationMessage.Substring(0, 300);
                }

                db.Set<Notification>().Add(new Notification
                {
                    UserId = trainer.UserId,
                    Message = notificationMessage,
                    Type = "Request",
                    IsRead = false,
                    CreatedAt = now
                });
            }

            db.SaveChanges();

            TempData["Success"] =
                "Your request has been submitted to your assigned trainer.";

            if (analysis.ShowEmergencyGuidance)
            {
                TempData["EmergencyWarning"] =
                    "Your message may describe a medical emergency. " +
                    "Do not wait for your trainer to respond. " +
                    "Stop exercising and seek immediate medical assistance. " +
                    "Contact your local emergency services if you are in immediate danger.";
            }
            else if (analysis.RequiresUrgentReview)
            {
                TempData["PriorityInfo"] =
                    "Your request has been flagged as high priority for trainer review. " +
                    "This does not guarantee an immediate response.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Close(int id)
        {
            var request = db.Set<TrainerRequest>()
                .FirstOrDefault(r =>
                    r.TrainerRequestId == id &&
                    r.ClientId == CurrentClientId);

            if (request == null)
            {
                return HttpNotFound();
            }

            if (request.Status == TrainerRequest.Statuses.Closed)
            {
                TempData["Info"] =
                    "This request is already closed.";

                return RedirectToAction("Index");
            }

            var canClose =
                request.Status == TrainerRequest.Statuses.Open ||
                request.Status == TrainerRequest.Statuses.InReview ||
                request.Status == TrainerRequest.Statuses.Answered;

            if (!canClose)
            {
                return new HttpStatusCodeResult(400);
            }

            var now = DateTime.Now;

            request.Status = TrainerRequest.Statuses.Closed;
            request.ClosedAt = now;
            request.ClosedByRole = "Client";
            request.UpdatedAt = now;

            // Only alert the current assigned trainer; historical requests remain private.
            if (CurrentClient.TrainerId.HasValue &&
                CurrentClient.TrainerId.Value == request.TrainerId)
            {
                var trainerUserId = (
                    from trainer in db.Set<TrainerProfile>()
                    join account in db.Set<ApplicationUser>()
                        on trainer.UserId equals account.UserId
                    where trainer.TrainerId == request.TrainerId && account.IsActive
                    select (int?)account.UserId
                ).FirstOrDefault();

                if (trainerUserId.HasValue)
                {
                    var message = "A client closed a trainer request: " + request.Subject;
                    if (message.Length > 300)
                        message = message.Substring(0, 300);

                    db.Set<Notification>().Add(new Notification
                    {
                        UserId = trainerUserId.Value,
                        Message = message,
                        Type = "Request",
                        IsRead = false,
                        CreatedAt = now
                    });
                }
            }

            db.SaveChanges();

            TempData["Success"] =
                "Request closed successfully.";

            return RedirectToAction("Index");
        }

        private ClientTrainerRequestViewModel BuildViewModel(
            ClientTrainerRequestForm form = null)
        {
            var clientId = CurrentClientId;

            var vm = new ClientTrainerRequestViewModel
            {
                HasAssignedTrainer = CurrentClient.TrainerId.HasValue
            };

            vm.RequestForm = form ?? new ClientTrainerRequestForm
            {
                Category = TrainerRequest.Categories.GeneralQuestion,
                Priority = TrainerRequest.Priorities.Normal
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

            var requests = (
                from r in db.Set<TrainerRequest>()
                join t in db.Set<TrainerProfile>()
                    on r.TrainerId equals t.TrainerId
                join u in db.Set<ApplicationUser>()
                    on t.UserId equals u.UserId
                where r.ClientId == clientId
                orderby r.CreatedAt descending,
                        r.TrainerRequestId descending
                select new ClientTrainerRequestItem
                {
                    TrainerRequestId = r.TrainerRequestId,
                    TrainerName = u.FirstName + " " + u.LastName,
                    Category = r.Category,
                    Priority = r.Priority,
                    Subject = r.Subject,
                    Description = r.Description,
                    Status = r.Status,
                    TrainerResponse = r.TrainerResponse,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    RespondedAt = r.RespondedAt,
                    ClosedAt = r.ClosedAt
                }
            ).ToList();

            vm.OpenCount = requests.Count(r =>
                r.Status == TrainerRequest.Statuses.Open);

            vm.InReviewCount = requests.Count(r =>
                r.Status == TrainerRequest.Statuses.InReview);

            vm.AnsweredCount = requests.Count(r =>
                r.Status == TrainerRequest.Statuses.Answered);

            vm.ClosedCount = requests.Count(r =>
                r.Status == TrainerRequest.Statuses.Closed);

            vm.Requests = requests;

            return vm;
        }
    }
}
