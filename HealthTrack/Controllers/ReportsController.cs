using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class ReportPoint
    {
        public int ClientId { get; set; }
        public DateTime When { get; set; }
        public decimal Value { get; set; }
    }

    public class ReportsController : TrainerBaseController
    {
        // GET: /Reports?days=7|30|90
        public ActionResult Index(int days = 30)
        {
            if (days != 7 && days != 90) days = 30;

            var tid = CurrentTrainerId;
            var now = DateTime.Now;
            var today = now.Date;
            var since = today.AddDays(-(days - 1));
            var fetchSince = today.AddDays(-Math.Max(days - 1, 55));   // enough history for the 8-week chart

            var clients = ClientRows(true);
            var ids = clients.Select(c => c.ClientId).ToList();

            List<ReportPoint> workouts = new List<ReportPoint>();
            List<ReportPoint> meals = new List<ReportPoint>();
            List<ReportPoint> weights = new List<ReportPoint>();

            if (ids.Count > 0)
            {
                // ===== DATA ACCESS: the only place that touches your log models. =====
                // If the build complains about WorkoutLog / MealLog / HealthMetric,
                // change the property names in these three queries to match your classes.
                workouts = db.Set<WorkoutLog>()
                    .Where(w => ids.Contains(w.ClientId) && w.Date >= fetchSince)
                    .Select(w => new ReportPoint { ClientId = w.ClientId, When = w.Date, Value = (decimal)w.CaloriesBurned })
                    .ToList();

                meals = db.Set<MealLog>()
                    .Where(m => ids.Contains(m.ClientId) && m.Date >= fetchSince)
                    .Select(m => new ReportPoint { ClientId = m.ClientId, When = m.Date, Value = (decimal)m.Calories })
                    .ToList();

                weights = db.Set<HealthMetric>()
                    .Where(h => ids.Contains(h.ClientId) && h.DateRecorded >= since)
                    .Select(h => new ReportPoint { ClientId = h.ClientId, When = h.DateRecorded, Value = (decimal)h.Weight })
                    .ToList();
                // ===== END DATA ACCESS =====
            }

            var vm = new ReportsViewModel { Days = days, ClientCount = clients.Count };

            foreach (var c in clients)
            {
                var cid = c.ClientId;
                var cw = workouts.Where(p => p.ClientId == cid && p.When >= since).ToList();
                var cm = meals.Where(p => p.ClientId == cid && p.When >= since).ToList();
                var cwt = weights.Where(p => p.ClientId == cid).OrderBy(p => p.When).ToList();

                decimal? avgIn = null;
                if (cm.Count > 0)
                {
                    var daysLogged = cm.Select(p => p.When.Date).Distinct().Count();
                    avgIn = Math.Round(cm.Sum(p => p.Value) / daysLogged, 0);
                }

                decimal? change = null;
                if (cwt.Count >= 2) change = Math.Round(cwt.Last().Value - cwt.First().Value, 1);

                var dates = workouts.Where(p => p.ClientId == cid).Select(p => p.When)
                    .Concat(meals.Where(p => p.ClientId == cid).Select(p => p.When)).ToList();
                DateTime? last = null;
                if (dates.Count > 0) last = dates.Max();

                vm.Rows.Add(new ReportRow
                {
                    ClientName = c.Name,
                    Initials = c.Initials,
                    Workouts = cw.Count,
                    CaloriesBurned = cw.Sum(p => p.Value),
                    AvgCaloriesIn = avgIn,
                    WeightChange = change,
                    LastActivity = last
                });

                if (cw.Count > 0 || cm.Count > 0) vm.ActiveClients++;
            }

            vm.TotalWorkouts = vm.Rows.Sum(r => r.Workouts);
            vm.TotalCaloriesBurned = vm.Rows.Sum(r => r.CaloriesBurned);

            var maxRow = Math.Max(1, vm.Rows.Count == 0 ? 1 : vm.Rows.Max(r => r.Workouts));
            foreach (var r in vm.Rows) r.Pct = (int)Math.Round(100.0 * r.Workouts / maxRow);
            vm.Rows = vm.Rows.OrderByDescending(r => r.Workouts).ThenBy(r => r.ClientName).ToList();

            vm.CompletedAppointments = db.Set<Appointment>().Count(a =>
                a.TrainerId == tid && a.Status == "Completed" &&
                a.AppointmentDateTime >= since && a.AppointmentDateTime <= now);

            // workouts logged per week, last 8 weeks (weeks start on Monday)
            var dow = ((int)today.DayOfWeek + 6) % 7;
            var thisMonday = today.AddDays(-dow);
            for (var i = 7; i >= 0; i--)
            {
                var start = thisMonday.AddDays(-7 * i);
                var end = start.AddDays(7);
                vm.Weeks.Add(new WeekBar
                {
                    Label = start.ToString("d MMM"),
                    Count = workouts.Count(p => p.When >= start && p.When < end)
                });
            }
            var maxWeek = Math.Max(1, vm.Weeks.Max(w => w.Count));
            foreach (var w in vm.Weeks) w.Pct = (int)Math.Round(100.0 * w.Count / maxWeek);

            return View(vm);
        }
    }
}
