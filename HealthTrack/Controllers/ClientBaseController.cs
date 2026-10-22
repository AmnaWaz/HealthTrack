
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

            CreateDueReminders(user.UserId, client.ClientId);

            ViewBag.SidePhoto = user.ProfilePhotoPath;
            ViewBag.SideUnread = db.Set<Notification>()
                .Count(n =>
                    n.UserId == user.UserId &&
                    !n.IsRead
                );

            base.OnActionExecuting(filterContext);
        }


        private void CreateDueReminders(int userId, int clientId)
        {
            if (!string.Equals(Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                return;

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var dayAfterTomorrow = today.AddDays(2);
            var dayKey = today.ToString("yyyy-MM-dd");

            var activeWorkoutPlan = db.Set<WorkoutPlan>().Any(p =>
                p.ClientId == clientId && p.StartDate <= today && p.EndDate >= today);
            var workoutLogged = db.Set<WorkoutLog>().Any(w =>
                w.ClientId == clientId && w.Date >= today && w.Date < tomorrow);

            if (activeWorkoutPlan && !workoutLogged)
                AddReminderOnce(userId, "Workout", "[DailyWorkout:" + dayKey + "] Your workout plan is active today. Remember to log your training.");

            var activeDietPlan = db.Set<DietPlan>().Any(p =>
                p.ClientId == clientId && p.StartDate <= today && p.EndDate >= today);
            var mealLogged = db.Set<MealLog>().Any(m =>
                m.ClientId == clientId && m.Date >= today && m.Date < tomorrow);

            if (activeDietPlan && !mealLogged)
                AddReminderOnce(userId, "Meal", "[DailyMeal:" + dayKey + "] Your diet plan is active today. Remember to record your meals.");

            var upcoming = db.Set<Appointment>()
                .Where(a => a.ClientId == clientId &&
                    (a.Status == "Scheduled" || a.Status == "Approved") &&
                    a.AppointmentDateTime >= today && a.AppointmentDateTime < dayAfterTomorrow)
                .Select(a => new { a.AppointmentId, a.AppointmentDateTime })
                .ToList();

            foreach (var appointment in upcoming)
            {
                if (appointment.AppointmentDateTime <= DateTime.Now)
                    continue;

                if (appointment.AppointmentDateTime > DateTime.Now.AddHours(24))
                    continue;

                var message = "[Appointment:" + appointment.AppointmentId + "] Upcoming trainer appointment on " +
                    appointment.AppointmentDateTime.ToString("dd MMM yyyy 'at' h:mm tt") + ".";
                AddReminderOnce(userId, "Appointment", message);
            }

            var healthcare = db.Set<HealthcareAppointment>()
                .Where(a => a.ClientId == clientId && a.Status == "Scheduled" &&
                    a.AppointmentDateTime >= today && a.AppointmentDateTime < dayAfterTomorrow)
                .Select(a => new { a.HealthcareAppointmentId, a.AppointmentDateTime }).ToList();
            foreach (var consultation in healthcare)
            {
                if (consultation.AppointmentDateTime > DateTime.Now &&
                    consultation.AppointmentDateTime <= DateTime.Now.AddHours(24))
                {
                    AddReminderOnce(userId, "Appointment", "[Healthcare:" + consultation.HealthcareAppointmentId +
                        "] Healthcare consultation on " + consultation.AppointmentDateTime.ToString("dd MMM yyyy 'at' h:mm tt") + ".");
                }
            }

            if (db.ChangeTracker.HasChanges())
                db.SaveChanges();
        }

        private void AddReminderOnce(int userId, string type, string message)
        {
            if (db.Set<Notification>().Any(n => n.UserId == userId && n.Type == type && n.Message == message))
                return;

            db.Set<Notification>().Add(new Notification
            {
                UserId = userId,
                Type = type,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.Now
            });
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
