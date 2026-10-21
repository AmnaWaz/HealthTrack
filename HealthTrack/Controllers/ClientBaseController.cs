
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using HealthTrack.Data;
using HealthTrack.Models;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = "Client")]
    public abstract class ClientBaseController : Controller
    {
        protected readonly HealthTrackContext db = new HealthTrackContext();

        protected ApplicationUser CurrentUser { get; private set; }
        protected ClientProfile CurrentClient { get; private set; }

        protected int CurrentClientId
        {
            get { return CurrentClient.ClientId; }
        }

        protected int CurrentUserId
        {
            get { return CurrentUser.UserId; }
        }

        protected string ClientName
        {
            get { return CurrentUser.FullName; }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var email = User.Identity.Name;

            var user = db.Set<ApplicationUser>()
                .FirstOrDefault(u => u.Email == email);

            if (user == null || !user.IsActive)
            {
                FormsAuthentication.SignOut();

                filterContext.Result = RedirectToAction(
                    "Login",
                    "Account"
                );

                return;
            }

            var client = db.Set<ClientProfile>()
                .FirstOrDefault(c => c.UserId == user.UserId);

            if (client == null)
            {
                filterContext.Result = new HttpNotFoundResult(
                    "No client profile exists for this account."
                );

                return;
            }

            CurrentUser = user;
            CurrentClient = client;

            var firstInitial = string.IsNullOrWhiteSpace(user.FirstName)
                ? "C"
                : user.FirstName.Substring(0, 1);

            var lastInitial = string.IsNullOrWhiteSpace(user.LastName)
                ? ""
                : user.LastName.Substring(0, 1);

            ViewBag.SideName = user.FullName;
            ViewBag.SideInitials = (
                firstInitial + lastInitial
            ).ToUpperInvariant();

            ViewBag.SideUnread = db.Set<Notification>()
                .Count(n =>
                    n.UserId == user.UserId &&
                    !n.IsRead
                );

            base.OnActionExecuting(filterContext);
        }

        protected bool OwnsWorkoutLog(int workoutLogId)
        {
            return db.Set<WorkoutLog>()
                .Any(w =>
                    w.WorkoutLogId == workoutLogId &&
                    w.ClientId == CurrentClientId
                );
        }

        protected bool OwnsMealLog(int mealLogId)
        {
            return db.Set<MealLog>()
                .Any(m =>
                    m.MealLogId == mealLogId &&
                    m.ClientId == CurrentClientId
                );
        }

        protected bool OwnsHealthMetric(int metricId)
        {
            return db.Set<HealthMetric>()
                .Any(m =>
                    m.MetricId == metricId &&
                    m.ClientId == CurrentClientId
                );
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
