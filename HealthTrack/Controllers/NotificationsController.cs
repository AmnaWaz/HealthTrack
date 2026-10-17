using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class NotificationsController : TrainerBaseController
    {
        // GET: /Notifications?filter=all|unread
        public ActionResult Index(string filter)
        {
            filter = (filter ?? "all").ToLower() == "unread" ? "unread" : "all";
            var uid = CurrentUserId;

            var mine = db.Set<Notification>().Where(n => n.UserId == uid);
            var vm = new NotificationsPageViewModel
            {
                Filter = filter,
                Total = mine.Count(),
                Unread = mine.Count(n => !n.IsRead)
            };

            var q = filter == "unread" ? mine.Where(n => !n.IsRead) : mine;
            vm.Items = q.OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToList()
                .Select(n => new NotificationItem
                {
                    Id = n.NotificationId,
                    Message = n.Message,
                    Type = n.Type,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead
                }).ToList();

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult MarkRead(int id, string filter)
        {
            var n = db.Set<Notification>().Find(id);
            if (n == null || n.UserId != CurrentUserId) return HttpNotFound();
            n.IsRead = true;
            db.SaveChanges();
            return RedirectToAction("Index", new { filter = filter });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult MarkAllRead(string filter)
        {
            var uid = CurrentUserId;
            var unread = db.Set<Notification>().Where(n => n.UserId == uid && !n.IsRead).ToList();
            foreach (var n in unread) n.IsRead = true;
            db.SaveChanges();
            return RedirectToAction("Index", new { filter = filter });
        }
    }
}
