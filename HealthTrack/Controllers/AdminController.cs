using System;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class AdminController : AdminBaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            var today = DateTime.Today;
            var start = today.AddDays(-6);
            var endExclusive = today.AddDays(1);
            var vm = new AdminOverviewVm
            {
                Clients = db.ClientProfiles.Count(),
                Trainers = db.TrainerProfiles.Count(),
                ActiveUsers = db.Users.Count(x => x.IsActive),
                Unassigned = db.ClientProfiles.Count(x => x.TrainerId == null),
                PendingSelections = db.TrainerSelectionRequests.Count(x => x.Status == TrainerSelectionRequest.Statuses.Pending),
                OpenAppointments = db.Appointments.Count(x => x.AppointmentDateTime >= DateTime.Now &&
                    (x.Status == AppointmentStatus.Approved || x.Status == AppointmentStatus.Scheduled))
            };
            var workouts = db.WorkoutLogs.Where(x => x.Date >= start && x.Date < endExclusive)
                .Select(x => x.Date).ToList();
            var meals = db.MealLogs.Where(x => x.Date >= start && x.Date < endExclusive)
                .Select(x => x.Date).ToList();
            for (var date = start; date <= today; date = date.AddDays(1))
                vm.Activity.Add(new AdminDailyRow
                {
                    Date = date,
                    Workouts = workouts.Count(x => x.Date.Date == date),
                    Meals = meals.Count(x => x.Date.Date == date)
                });
            vm.RecentUsers = db.Users.OrderByDescending(x => x.DateJoined)
                .Take(8).Select(x => new { x.FirstName, x.LastName, x.Email, x.DateJoined, x.IsActive, Role = x.Role.RoleName })
                .ToList().Select(x => new AdminUserRow
                {
                    Name = (x.FirstName + " " + x.LastName).Trim(), Email = x.Email, Role = x.Role,
                    Joined = x.DateJoined, IsActive = x.IsActive
                }).ToList();
            return View(vm);
        }

        [HttpGet]
        public ActionResult Reports(DateTime? from, DateTime? to)
        {
            DateTime start, end;
            if (!TryPeriod(from, to, out start, out end)) return new HttpStatusCodeResult(400, "Invalid reporting period");
            return View(BuildReport(start, end));
        }

        [HttpGet]
        public ActionResult ReportsCsv(DateTime? from, DateTime? to)
        {
            DateTime start, end;
            if (!TryPeriod(from, to, out start, out end)) return new HttpStatusCodeResult(400, "Invalid reporting period");
            var report = BuildReport(start, end);
            var sb = new StringBuilder();
            sb.AppendLine("HealthTrack administrative report");
            sb.AppendLine("From," + start.ToString("yyyy-MM-dd") + ",To," + end.ToString("yyyy-MM-dd"));
            sb.AppendLine("Workout entries," + report.WorkoutCount.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("Meal entries," + report.MealCount.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("Completed appointments," + report.CompletedAppointments.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine(); sb.AppendLine("Date,Workout logs,Meal logs");
            foreach (var d in report.Daily) sb.AppendLine(d.Date.ToString("yyyy-MM-dd") + "," + d.Workouts + "," + d.Meals);
            sb.AppendLine(); sb.AppendLine("Exercise,Logged count");
            foreach (var e in report.Exercises) sb.AppendLine(Csv(e.Exercise) + "," + e.Count);
            sb.AppendLine(); sb.AppendLine("Trainer,Assigned clients,Workout plans,Diet plans,Appointments");
            foreach (var t in report.Trainers)
                sb.AppendLine(Csv(t.Trainer) + "," + t.Clients + "," + t.WorkoutPlans + "," + t.DietPlans + "," + t.Appointments);
            return File(new UTF8Encoding(true).GetBytes(sb.ToString()), "text/csv", "healthtrack-report-" + end.ToString("yyyyMMdd") + ".csv");
        }

        private static string Csv(string s)
        {
            var value = (s ?? "").Replace("\r", " ").Replace("\n", " ");
            if (value.Length > 0 && "=+-@".Contains(value[0])) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static bool TryPeriod(DateTime? from, DateTime? to, out DateTime start, out DateTime end)
        {
            end = (to ?? DateTime.Today).Date;
            start = (from ?? end.AddDays(-29)).Date;
            return start <= end && end <= DateTime.Today && (end - start).TotalDays <= 365;
        }

        private AdminReportsVm BuildReport(DateTime from, DateTime to)
        {
            var exclusiveEnd = to.AddDays(1);
            var workoutRows = db.WorkoutLogs.Where(x => x.Date >= from && x.Date < exclusiveEnd)
                .Select(x => new { x.Date, x.ClientId, x.Exercise }).ToList();
            var mealRows = db.MealLogs.Where(x => x.Date >= from && x.Date < exclusiveEnd)
                .Select(x => new { x.Date, x.ClientId }).ToList();
            var vm = new AdminReportsVm
            {
                From = from, To = to, WorkoutCount = workoutRows.Count, MealCount = mealRows.Count,
                ActiveWorkoutClients = workoutRows.Select(x => x.ClientId).Distinct().Count(),
                ActiveMealClients = mealRows.Select(x => x.ClientId).Distinct().Count(),
                CompletedAppointments = db.Appointments.Count(x => x.Status == AppointmentStatus.Completed &&
                    x.AppointmentDateTime >= from && x.AppointmentDateTime < exclusiveEnd)
            };
            var workoutDates = workoutRows.GroupBy(x => x.Date.Date).ToDictionary(g => g.Key, g => g.Count());
            var mealDates = mealRows.GroupBy(x => x.Date.Date).ToDictionary(g => g.Key, g => g.Count());
            for (var date = from; date <= to; date = date.AddDays(1))
                vm.Daily.Add(new AdminDailyRow
                {
                    Date = date,
                    Workouts = workoutDates.ContainsKey(date) ? workoutDates[date] : 0,
                    Meals = mealDates.ContainsKey(date) ? mealDates[date] : 0
                });
            vm.Exercises = workoutRows.Where(x => !string.IsNullOrWhiteSpace(x.Exercise))
                .GroupBy(x => x.Exercise.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(x => x.Count()).ThenBy(x => x.Key).Take(10)
                .Select(x => new AdminExerciseRow { Exercise = x.Key, Count = x.Count() }).ToList();
            var trainers = db.TrainerProfiles.Select(t => new
            {
                t.TrainerId, t.User.FirstName, t.User.LastName,
                Clients = db.ClientProfiles.Count(c => c.TrainerId == t.TrainerId),
                WorkoutPlans = db.WorkoutPlans.Count(p => p.TrainerId == t.TrainerId),
                DietPlans = db.DietPlans.Count(p => p.TrainerId == t.TrainerId)
            }).ToList();
            var sessionCounts = db.Appointments.Where(a => a.AppointmentDateTime >= from && a.AppointmentDateTime < exclusiveEnd)
                .GroupBy(a => a.TrainerId).Select(g => new { TrainerId = g.Key, Count = g.Count() }).ToList()
                .ToDictionary(x => x.TrainerId, x => x.Count);
            vm.Trainers = trainers.Select(t => new AdminTrainerReportRow
            {
                Trainer = (t.FirstName + " " + t.LastName).Trim(), Clients = t.Clients,
                WorkoutPlans = t.WorkoutPlans, DietPlans = t.DietPlans,
                Appointments = sessionCounts.ContainsKey(t.TrainerId) ? sessionCounts[t.TrainerId] : 0
            }).OrderBy(x => x.Trainer).ToList();
            return vm;
        }

        [HttpGet]
        public ActionResult Users(string q, string role, string status, int page = 1)
        {
            page = Math.Max(1, Math.Min(page, 10000));
            var query = db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(u => u.FirstName.Contains(term) || u.LastName.Contains(term) || u.Email.Contains(term) || u.Phone.Contains(term));
            }
            if (role == Role.Client || role == Role.Trainer || role == Role.Admin)
                query = query.Where(u => u.Role.RoleName == role);
            if (status == "active") query = query.Where(u => u.IsActive);
            if (status == "inactive") query = query.Where(u => !u.IsActive);
            var vm = new AdminUsersVm { Query = q, Role = role, Status = status, Page = page };
            vm.Total = query.Count();
            var lastPage = Math.Max(1, (vm.Total + 19) / 20);
            vm.Page = Math.Min(page, lastPage);
            vm.Users = query.OrderBy(u => u.UserId).Skip((vm.Page - 1) * 20).Take(20)
                .Select(u => new AdminAccountRow { UserId = u.UserId, Name = u.FirstName + " " + u.LastName,
                    Email = u.Email, Role = u.Role.RoleName, IsActive = u.IsActive, Joined = u.DateJoined }) .ToList();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SetUserStatus(int userId, bool active)
        {
            using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var user = db.Users.Include("Role").FirstOrDefault(x => x.UserId == userId);
                if (user == null) return HttpNotFound();
                if (!active && user.UserId == AdminUser.UserId)
                { TempData["AdminError"] = "You cannot deactivate your own account."; return RedirectToAction("Users"); }
                if (!active && user.Role.RoleName == Role.Admin &&
                    db.Users.Count(x => x.IsActive && x.Role.RoleName == Role.Admin) <= 1)
                { TempData["AdminError"] = "At least one active administrator is required."; return RedirectToAction("Users"); }
                if (!active && user.Role.RoleName == Role.Trainer)
                {
                    var trainerId = db.TrainerProfiles.Where(t => t.UserId == userId)
                        .Select(t => (int?)t.TrainerId).FirstOrDefault();
                    if (trainerId.HasValue && db.ClientProfiles.Any(c => c.TrainerId == trainerId.Value))
                    {
                        TempData["AdminError"] = "This trainer still has assigned clients. Reassign or unassign them first.";
                        return RedirectToAction("Users");
                    }
                }
                if (user.IsActive == active)
                {
                    TempData["AdminNotice"] = "This account already has the selected status.";
                    return RedirectToAction("Users");
                }
                user.IsActive = active;
                if (!active)
                {
                    int? clientId = db.ClientProfiles.Where(x => x.UserId == userId).Select(x => (int?)x.ClientId).FirstOrDefault();
                    int? trainerId = db.TrainerProfiles.Where(x => x.UserId == userId).Select(x => (int?)x.TrainerId).FirstOrDefault();
                    var pending = db.TrainerSelectionRequests.Where(x => x.Status == TrainerSelectionRequest.Statuses.Pending &&
                        (clientId.HasValue && x.ClientId == clientId.Value || trainerId.HasValue && x.TrainerId == trainerId.Value)).ToList();
                    var now = DateTime.Now;
                    foreach (var request in pending) { request.Status = TrainerSelectionRequest.Statuses.Cancelled; request.RespondedAt = now; }
                    if (clientId.HasValue)
                    {
                        var futureAppointments = db.Appointments.Where(x => x.ClientId == clientId.Value && x.AppointmentDateTime > now &&
                            (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Scheduled || x.Status == AppointmentStatus.Approved)).ToList();
                        var affectedTrainerIds = futureAppointments.Select(x => x.TrainerId).Distinct().ToList();
                        foreach (var appointment in futureAppointments) appointment.Status = AppointmentStatus.Cancelled;
                        if (affectedTrainerIds.Count > 0)
                        {
                            var affectedUsers = db.TrainerProfiles.Where(x => affectedTrainerIds.Contains(x.TrainerId))
                                .Select(x => x.UserId).Distinct().ToList();
                            foreach (var recipientId in affectedUsers)
                                db.Notifications.Add(new Notification { UserId = recipientId, Type = "Appointment",
                                    Message = "A client's account was deactivated. Their future appointments with you were cancelled.",
                                    CreatedAt = now, IsRead = false });
                        }
                    }
                    else if (trainerId.HasValue)
                    {
                        var futureAppointments = db.Appointments.Where(x => x.TrainerId == trainerId.Value && x.AppointmentDateTime > now &&
                            (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Scheduled || x.Status == AppointmentStatus.Approved)).ToList();
                        var affectedClientIds = futureAppointments.Select(x => x.ClientId).Distinct().ToList();
                        foreach (var appointment in futureAppointments) appointment.Status = AppointmentStatus.Cancelled;
                        if (affectedClientIds.Count > 0)
                        {
                            var affectedUsers = db.ClientProfiles.Where(x => affectedClientIds.Contains(x.ClientId))
                                .Select(x => x.UserId).Distinct().ToList();
                            foreach (var recipientId in affectedUsers)
                                db.Notifications.Add(new Notification { UserId = recipientId, Type = "Appointment",
                                    Message = "Your upcoming appointment was cancelled because your trainer's account is inactive.",
                                    CreatedAt = now, IsRead = false });
                        }
                    }
                }
                db.SaveChanges(); tx.Commit();
            }
            TempData["AdminNotice"] = active ? "Account activated." : "Account deactivated.";
            return RedirectToAction("Users");
        }

        [HttpGet]
        public ActionResult Assignments(string tab = "pending")
        {
            if (tab != "pending" && tab != "assigned" && tab != "unassigned") tab = "pending";
            var vm = new AdminAssignmentsVm { Tab = tab };
            vm.Trainers = db.TrainerProfiles.Where(x => x.User.IsActive).OrderBy(x => x.User.FirstName)
                .Select(x => new AdminTrainerOption { TrainerId = x.TrainerId, Name = x.User.FirstName + " " + x.User.LastName }).ToList();
            vm.Clients = db.ClientProfiles.Where(x => x.User.IsActive).OrderBy(x => x.User.FirstName).Select(x => new AdminClientAssignmentRow
            {
                ClientId = x.ClientId, Client = x.User.FirstName + " " + x.User.LastName,
                TrainerId = x.TrainerId, Trainer = x.Trainer == null ? "Unassigned" : x.Trainer.User.FirstName + " " + x.Trainer.User.LastName
            }).ToList();
            vm.Pending = db.TrainerSelectionRequests.Where(x => x.Status == TrainerSelectionRequest.Statuses.Pending && x.Client.User.IsActive && x.Trainer.User.IsActive)
                .OrderBy(x => x.CreatedAt).Select(x => new AdminPendingSelectionRow
                {
                    RequestId = x.TrainerSelectionRequestId, Client = x.Client.User.FirstName + " " + x.Client.User.LastName,
                    Trainer = x.Trainer.User.FirstName + " " + x.Trainer.User.LastName, Created = x.CreatedAt
                }).ToList();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ChangeAssignment(int clientId, int? trainerId)
        {
            using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                LockClientAssignment(clientId);
                var client = db.ClientProfiles.Include("User").FirstOrDefault(x => x.ClientId == clientId);
                if (client == null) return HttpNotFound();
                if (!client.User.IsActive) { TempData["AdminError"] = "The client account is inactive."; return RedirectToAction("Assignments"); }
                TrainerProfile trainer = null;
                if (trainerId.HasValue)
                {
                    trainer = db.TrainerProfiles.Include("User").FirstOrDefault(x => x.TrainerId == trainerId.Value);
                    if (trainer == null || !trainer.User.IsActive)
                    { TempData["AdminError"] = "Choose an active trainer."; return RedirectToAction("Assignments"); }
                }
                var previous = client.TrainerId;
                if (previous == trainerId) { TempData["AdminNotice"] = "Assignment is unchanged."; return RedirectToAction("Assignments"); }
                var now = DateTime.Now;
                client.TrainerId = trainerId;
                var pending = db.TrainerSelectionRequests.Where(x => x.ClientId == clientId && x.Status == TrainerSelectionRequest.Statuses.Pending).ToList();
                foreach (var request in pending) { request.Status = TrainerSelectionRequest.Statuses.Cancelled; request.RespondedAt = now; }
                // Preserve unrelated historical records and appointments when there is no previous trainer.
                var appointments = previous.HasValue
                    ? db.Appointments.Where(x => x.ClientId == clientId && x.TrainerId == previous.Value && x.AppointmentDateTime > now &&
                        (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Scheduled || x.Status == AppointmentStatus.Approved)).ToList()
                    : new List<Appointment>();
                foreach (var appointment in appointments) appointment.Status = AppointmentStatus.Cancelled;
                var message = trainer == null ? "Your trainer assignment was removed by an administrator." :
                    "Your trainer assignment has changed. Open My Trainer to see your new trainer.";
                db.Notifications.Add(new Notification { UserId = client.UserId, Message = message, Type = "TrainerSelection", CreatedAt = now, IsRead = false });
                var previousUser = previous.HasValue ? db.TrainerProfiles.Where(x => x.TrainerId == previous.Value).Select(x => (int?)x.UserId).FirstOrDefault() : null;
                if (previousUser.HasValue) db.Notifications.Add(new Notification { UserId = previousUser.Value,
                    Message = "An administrator updated your client assignments. Check My Clients.", Type = "TrainerSelection", CreatedAt = now });
                if (trainer != null) db.Notifications.Add(new Notification { UserId = trainer.UserId,
                    Message = "An administrator assigned a client to you. Check My Clients.", Type = "TrainerSelection", CreatedAt = now });
                db.SaveChanges(); tx.Commit();
            }
            TempData["AdminNotice"] = "Assignment updated. Any future appointments with the former trainer were cancelled.";
            return RedirectToAction("Assignments");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CancelSelection(int requestId)
        {
            using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var request = db.TrainerSelectionRequests.FirstOrDefault(x => x.TrainerSelectionRequestId == requestId);
                if (request == null) return HttpNotFound();
                if (request.Status != TrainerSelectionRequest.Statuses.Pending)
                {
                    TempData["AdminNotice"] = "This request has already been handled.";
                    return RedirectToAction("Assignments");
                }
                var now = DateTime.Now;
                request.Status = TrainerSelectionRequest.Statuses.Cancelled;
                request.RespondedAt = now;
                var clientUserId = db.ClientProfiles.Where(c => c.ClientId == request.ClientId)
                    .Select(c => (int?)c.UserId).FirstOrDefault();
                var trainerUserId = db.TrainerProfiles.Where(t => t.TrainerId == request.TrainerId)
                    .Select(t => (int?)t.UserId).FirstOrDefault();
                if (clientUserId.HasValue)
                    db.Notifications.Add(new Notification { UserId = clientUserId.Value, Type = "TrainerSelection",
                        Message = "An administrator cancelled a pending trainer request.", CreatedAt = now, IsRead = false });
                if (trainerUserId.HasValue)
                    db.Notifications.Add(new Notification { UserId = trainerUserId.Value, Type = "TrainerSelection",
                        Message = "An administrator cancelled a pending client selection request.", CreatedAt = now, IsRead = false });
                db.SaveChanges();
                tx.Commit();
            }
            TempData["AdminNotice"] = "Pending request cancelled. Both accounts were notified.";
            return RedirectToAction("Assignments");
        }
        private void LockClientAssignment(int clientId)
        {
            db.Database.ExecuteSqlCommand(@"DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource = {0}, @LockMode = 'Exclusive',
    @LockOwner = 'Transaction', @LockTimeout = 10000;
IF @result < 0 THROW 51001, 'Client assignment is currently being updated. Please retry.', 1;",
                "HealthTrack:ClientAssignment:" + clientId.ToString(CultureInfo.InvariantCulture));
        }

    }
}
