using System;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    // Trainer writes feedback for a client. It is delivered to the client as a Notification of type "Feedback".
    public class FeedbackController : TrainerBaseController
    {
        // GET: /Feedback?clientId=5
        public ActionResult Index(int? clientId)
        {
            return View(Build(new FeedbackPageViewModel { ClientId = clientId }));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Index(FeedbackPageViewModel vm)
        {
            if (vm.ClientId.HasValue && !IsMyClient(vm.ClientId.Value))
                ModelState.AddModelError("ClientId", "You can only send feedback to your own clients.");

            if (!ModelState.IsValid) return View(Build(vm));

            Notify(vm.ClientId.Value, "Feedback from " + TrainerName + ": " + vm.Message.Trim(), "Feedback");
            db.SaveChanges();

            TempData["Success"] = "Feedback sent.";
            return RedirectToAction("Index");
        }

        private FeedbackPageViewModel Build(FeedbackPageViewModel vm)
        {
            var tid = CurrentTrainerId;
            vm.Clients = MyClientOptions();
            vm.AverageRating = (decimal)CurrentTrainer.AverageRating;

            var sent = (from n in db.Set<Notification>()
                        join c in db.Set<ClientProfile>() on n.UserId equals c.UserId
                        join u in db.Set<ApplicationUser>() on c.UserId equals u.UserId
                        where c.TrainerId == tid && n.Type == "Feedback"
                        orderby n.CreatedAt descending
                        select new { u.FirstName, u.LastName, n.Message, n.CreatedAt })
                       .Take(20).ToList();

            vm.Sent = sent.Select(s => new FeedbackRow
            {
                ClientName = s.FirstName + " " + s.LastName,
                Message = s.Message,
                CreatedAt = s.CreatedAt
            }).ToList();
            return vm;
        }
    }
}
