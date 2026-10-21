
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class ClientNotificationsController : ClientBaseController
    {
        [HttpGet]
        public ActionResult Index(string filter)
        {
            filter = (filter ?? "all").ToLowerInvariant() == "unread"
                ? "unread"
                : "all";

            var userId = CurrentUserId;

            var mine = db.Set<Notification>()
                .Where(n => n.UserId == userId);

            var totalCount = mine.Count();
            var unreadCount = mine.Count(n => !n.IsRead);

            var query = filter == "unread"
                ? mine.Where(n => !n.IsRead)
                : mine;

            var notifications = query
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.NotificationId)
                .Take(100)
                .Select(n => new
                {
                    n.NotificationId,
                    n.Message,
                    n.Type,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToList();

            var vm = new ClientNotificationsViewModel
            {
                FirstName = CurrentUser.FirstName,
                Filter = filter,
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                ReadCount = totalCount - unreadCount,
                Notifications = notifications
                    .Select(n => new ClientNotificationItem
                    {
                        NotificationId = n.NotificationId,
                        Message = n.Message,
                        Type = n.Type,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkRead(int id, string filter)
        {
            var notification = db.Set<Notification>()
                .FirstOrDefault(n =>
                    n.NotificationId == id &&
                    n.UserId == CurrentUserId);

            if (notification == null)
            {
                return HttpNotFound();
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                db.SaveChanges();
            }

            return RedirectToAction("Index", new
            {
                filter = NormalizeFilter(filter)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAllRead(string filter)
        {
            var userId = CurrentUserId;

            var unread = db.Set<Notification>()
                .Where(n =>
                    n.UserId == userId &&
                    !n.IsRead)
                .ToList();

            foreach (var notification in unread)
            {
                notification.IsRead = true;
            }

            if (unread.Any())
            {
                db.SaveChanges();
            }

            return RedirectToAction("Index", new
            {
                filter = NormalizeFilter(filter)
            });
        }

        private static string NormalizeFilter(string filter)
        {
            return (filter ?? "").ToLowerInvariant() == "unread"
                ? "unread"
                : "all";
        }
    }
}
