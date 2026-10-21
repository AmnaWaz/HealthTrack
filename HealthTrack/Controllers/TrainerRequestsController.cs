
using System;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class TrainerRequestsController : TrainerBaseController
    {
        [HttpGet]
        public ActionResult Index(string filter)
        {
            return View(BuildViewModel(filter));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkInReview(int id, string filter)
        {
            var request = db.Set<TrainerRequest>()
                .FirstOrDefault(r =>
                    r.TrainerRequestId == id &&
                    r.TrainerId == CurrentTrainerId);

            if (request == null)
            {
                return HttpNotFound();
            }

            if (request.Status != TrainerRequest.Statuses.Open)
            {
                TempData["Info"] = "Only open requests can be marked in review.";
                return RedirectToAction("Index", new { filter = NormalizeFilter(filter) });
            }

            request.Status = TrainerRequest.Statuses.InReview;
            request.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["Success"] = "Request marked as in review.";

            return RedirectToAction("Index", new { filter = NormalizeFilter(filter) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Respond(TrainerRequestResponseForm form, string filter)
        {
            if (form == null)
            {
                return new HttpStatusCodeResult(400);
            }

            var request = db.Set<TrainerRequest>()
                .FirstOrDefault(r =>
                    r.TrainerRequestId == form.TrainerRequestId &&
                    r.TrainerId == CurrentTrainerId);

            if (request == null)
            {
                return HttpNotFound();
            }

            var canRespond =
                request.Status == TrainerRequest.Statuses.Open ||
                request.Status == TrainerRequest.Statuses.InReview;

            if (!canRespond ||
                !string.IsNullOrWhiteSpace(request.TrainerResponse))
            {
                TempData["Info"] = "This request cannot receive another response.";
                return RedirectToAction("Index", new { filter = NormalizeFilter(filter) });
            }

            if (string.IsNullOrWhiteSpace(form.TrainerResponse))
            {
                ModelState.AddModelError(
                    "TrainerResponse",
                    "Enter a response for your client."
                );
            }
            else if (form.TrainerResponse.Trim().Length < 5)
            {
                ModelState.AddModelError(
                    "TrainerResponse",
                    "Your response must contain at least five characters."
                );
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Enter a valid response between 5 and 1000 characters.";
                return RedirectToAction("Index", new { filter = NormalizeFilter(filter) });
            }

            var now = DateTime.Now;

            request.TrainerResponse = form.TrainerResponse.Trim();
            request.Status = TrainerRequest.Statuses.Answered;
            request.RespondedAt = now;
            request.UpdatedAt = now;

            var client = db.Set<ClientProfile>()
                .FirstOrDefault(c => c.ClientId == request.ClientId);

            if (client != null)
            {
                var notificationMessage =
                    "Your trainer has responded to your request: " +
                    request.Subject;

                if (notificationMessage.Length > 300)
                {
                    notificationMessage = notificationMessage.Substring(0, 300);
                }

                db.Set<Notification>().Add(new Notification
                {
                    UserId = client.UserId,
                    Message = notificationMessage,
                    Type = "Request",
                    IsRead = false,
                    CreatedAt = now
                });
            }

            db.SaveChanges();

            TempData["Success"] = "Response sent successfully.";

            return RedirectToAction("Index", new { filter = NormalizeFilter(filter) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Close(int id, string filter)
        {
            var request = db.Set<TrainerRequest>()
                .FirstOrDefault(r =>
                    r.TrainerRequestId == id &&
                    r.TrainerId == CurrentTrainerId);

            if (request == null)
            {
                return HttpNotFound();
            }

            if (request.Status != TrainerRequest.Statuses.Answered)
            {
                TempData["Info"] = "Only answered requests can be closed.";
                return RedirectToAction("Index", new { filter = NormalizeFilter(filter) });
            }

            var now = DateTime.Now;

            request.Status = TrainerRequest.Statuses.Closed;
            request.ClosedAt = now;
            request.ClosedByRole = "Trainer";
            request.UpdatedAt = now;

            db.SaveChanges();

            TempData["Success"] = "Request closed successfully.";

            return RedirectToAction("Index", new { filter = NormalizeFilter(filter) });
        }

        private TrainerRequestsViewModel BuildViewModel(string filter)
        {
            var trainerId = CurrentTrainerId;
            var selectedFilter = NormalizeFilter(filter);

            var requests = (
                from r in db.Set<TrainerRequest>()
                join c in db.Set<ClientProfile>()
                    on r.ClientId equals c.ClientId
                join u in db.Set<ApplicationUser>()
                    on c.UserId equals u.UserId
                where r.TrainerId == trainerId
                orderby r.CreatedAt descending, r.TrainerRequestId descending
                select new TrainerRequestInboxItem
                {
                    TrainerRequestId = r.TrainerRequestId,
                    ClientId = r.ClientId,
                    ClientName = u.FirstName + " " + u.LastName,
                    Category = r.Category,
                    Priority = r.Priority,
                    Subject = r.Subject,
                    Description = r.Description,
                    Status = r.Status,
                    TrainerResponse = r.TrainerResponse,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    RespondedAt = r.RespondedAt,
                    ClosedAt = r.ClosedAt,
                    ClosedByRole = r.ClosedByRole
                }
            ).ToList();

            var vm = new TrainerRequestsViewModel
            {
                TrainerName = TrainerName,
                Filter = selectedFilter,
                TotalCount = requests.Count,
                OpenCount = requests.Count(r =>
                    r.Status == TrainerRequest.Statuses.Open),
                InReviewCount = requests.Count(r =>
                    r.Status == TrainerRequest.Statuses.InReview),
                AnsweredCount = requests.Count(r =>
                    r.Status == TrainerRequest.Statuses.Answered),
                ClosedCount = requests.Count(r =>
                    r.Status == TrainerRequest.Statuses.Closed)
            };

            if (selectedFilter == "open")
            {
                vm.Requests = requests
                    .Where(r => r.Status == TrainerRequest.Statuses.Open)
                    .ToList();
            }
            else if (selectedFilter == "review")
            {
                vm.Requests = requests
                    .Where(r => r.Status == TrainerRequest.Statuses.InReview)
                    .ToList();
            }
            else if (selectedFilter == "answered")
            {
                vm.Requests = requests
                    .Where(r => r.Status == TrainerRequest.Statuses.Answered)
                    .ToList();
            }
            else if (selectedFilter == "closed")
            {
                vm.Requests = requests
                    .Where(r => r.Status == TrainerRequest.Statuses.Closed)
                    .ToList();
            }
            else
            {
                vm.Requests = requests;
            }

            return vm;
        }

        private static string NormalizeFilter(string filter)
        {
            switch ((filter ?? "").Trim().ToLowerInvariant())
            {
                case "open":
                    return "open";

                case "review":
                    return "review";

                case "answered":
                    return "answered";

                case "closed":
                    return "closed";

                default:
                    return "all";
            }
        }
    }
}
