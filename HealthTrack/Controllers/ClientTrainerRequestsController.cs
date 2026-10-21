
using System;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
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
        public ActionResult Submit(ClientTrainerRequestForm form)
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

                if (!db.Set<TrainerProfile>()
                    .Any(t => t.TrainerId == trainerId))
                {
                    ModelState.AddModelError(
                        "",
                        "Your assigned trainer could not be found."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                return View("Index", BuildViewModel(form));
            }

            var now = DateTime.Now;

            var request = new TrainerRequest
            {
                ClientId = CurrentClientId,
                TrainerId = CurrentClient.TrainerId.Value,
                Category = form.Category,
                Priority = form.Priority,
                Subject = form.Subject.Trim(),
                Description = form.Description.Trim(),
                Status = TrainerRequest.Statuses.Open,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.Set<TrainerRequest>().Add(request);
            db.SaveChanges();

            TempData["Success"] = "Your request has been submitted to your assigned trainer.";

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
                TempData["Info"] = "This request is already closed.";
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

            db.SaveChanges();

            TempData["Success"] = "Request closed successfully.";

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

            if (form != null)
            {
                vm.RequestForm = form;
            }
            else
            {
                vm.RequestForm = new ClientTrainerRequestForm
                {
                    Category = TrainerRequest.Categories.GeneralQuestion,
                    Priority = TrainerRequest.Priorities.Normal
                };
            }

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
                orderby r.CreatedAt descending, r.TrainerRequestId descending
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
