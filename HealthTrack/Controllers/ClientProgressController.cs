
using System;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class ClientProgressController : ClientBaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View(BuildPage());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(ClientMeasurementForm form)
        {
            HealthMetric metric = null;

            if (form.MetricId > 0)
            {
                metric = db.Set<HealthMetric>()
                    .FirstOrDefault(m =>
                        m.MetricId == form.MetricId &&
                        m.ClientId == CurrentClientId);

                if (metric == null)
                    return HttpNotFound();
            }

            if (!form.DateRecorded.HasValue ||
                form.DateRecorded.Value.Date > DateTime.Today ||
                form.DateRecorded.Value.Date < DateTime.Today.AddYears(-1))
            {
                ModelState.AddModelError(
                    "DateRecorded",
                    "Choose a date within the past year."
                );
            }

            if (CurrentClient.Height <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "A valid profile height is required to calculate BMI."
                );
            }

            if (form.DateRecorded.HasValue)
            {
                var date = form.DateRecorded.Value.Date;
                var nextDate = date.AddDays(1);

                var duplicate = db.Set<HealthMetric>()
                    .Any(m =>
                        m.ClientId == CurrentClientId &&
                        m.DateRecorded >= date &&
                        m.DateRecorded < nextDate &&
                        m.MetricId != form.MetricId);

                if (duplicate)
                {
                    ModelState.AddModelError(
                        "DateRecorded",
                        "A measurement already exists for this date. Edit the existing entry instead."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                var vm = BuildPage();
                vm.MeasurementForm = form;
                return View("Index", vm);
            }

            var heightMetres = CurrentClient.Height / 100m;
            var bmi = Math.Round(
                form.Weight / (heightMetres * heightMetres),
                2,
                MidpointRounding.AwayFromZero
            );

            if (metric == null)
            {
                metric = new HealthMetric
                {
                    ClientId = CurrentClientId
                };

                db.Set<HealthMetric>().Add(metric);
            }

            metric.DateRecorded = form.DateRecorded.Value.Date;
            metric.Weight = form.Weight;
            metric.BMI = bmi;
            metric.BodyFat = form.BodyFat ?? 0m;

            db.SaveChanges();

            SyncProfileWeight();

            TempData["Success"] = "Measurement saved successfully.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var metric = db.Set<HealthMetric>()
                .FirstOrDefault(m =>
                    m.MetricId == id &&
                    m.ClientId == CurrentClientId);

            if (metric == null)
                return HttpNotFound();

            db.Set<HealthMetric>().Remove(metric);
            db.SaveChanges();

            SyncProfileWeight();

            TempData["Success"] = "Measurement deleted successfully.";

            return RedirectToAction("Index");
        }

        private void SyncProfileWeight()
        {
            var latest = db.Set<HealthMetric>()
                .Where(m => m.ClientId == CurrentClientId)
                .OrderByDescending(m => m.DateRecorded)
                .ThenByDescending(m => m.MetricId)
                .FirstOrDefault();

            if (latest != null && CurrentClient.Weight != latest.Weight)
            {
                CurrentClient.Weight = latest.Weight;
                db.SaveChanges();
            }
        }

        private ClientProgressViewModel BuildPage()
        {
            var clientId = CurrentClientId;

            var measurements = db.Set<HealthMetric>()
                .Where(m => m.ClientId == clientId)
                .OrderByDescending(m => m.DateRecorded)
                .ThenByDescending(m => m.MetricId)
                .ToList();

            var workouts = db.Set<WorkoutLog>()
                .Where(w => w.ClientId == clientId)
                .OrderByDescending(w => w.Date)
                .ThenByDescending(w => w.WorkoutLogId)
                .ToList();

            var latest = measurements.FirstOrDefault();
            var earliest = measurements.LastOrDefault();

            var currentWeight = latest != null
                ? latest.Weight
                : CurrentClient.Weight;

            decimal? currentBmi = null;

            if (CurrentClient.Height > 0)
            {
                var heightMetres = CurrentClient.Height / 100m;

                currentBmi = Math.Round(
                    currentWeight / (heightMetres * heightMetres),
                    2,
                    MidpointRounding.AwayFromZero
                );
            }

            var vm = new ClientProgressViewModel
            {
                FirstName = CurrentUser.FirstName,
                HeightCm = CurrentClient.Height,
                CurrentWeight = currentWeight,
                CurrentBMI = currentBmi,
                CurrentBodyFat = latest != null && latest.BodyFat > 0
                    ? (decimal?)latest.BodyFat
                    : null,
                LatestMeasurementDate = latest != null
                    ? (DateTime?)latest.DateRecorded
                    : null,
                WeightIsFromProfile = latest == null,
                TotalMeasurements = measurements.Count,
                TotalTrainingDays = workouts
                    .Select(w => w.Date.Date)
                    .Distinct()
                    .Count(),
                TotalExerciseEntries = workouts.Count,
                TotalWorkoutMinutes = workouts.Sum(w => w.Duration),
                TotalRecordedCaloriesBurned = workouts.Sum(w => w.CaloriesBurned),
                WeightChange = latest != null &&
                               earliest != null &&
                               measurements.Count > 1
                    ? (decimal?)(latest.Weight - earliest.Weight)
                    : null
            };

            vm.Measurements = measurements
                .Select(m => new ClientMeasurementItem
                {
                    MetricId = m.MetricId,
                    DateRecorded = m.DateRecorded,
                    Weight = m.Weight,
                    BMI = m.BMI,
                    BodyFat = m.BodyFat > 0
                        ? (decimal?)m.BodyFat
                        : null
                })
                .ToList();

            vm.WorkoutActivity = workouts
                .GroupBy(w => w.Date.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new ClientWorkoutProgressItem
                {
                    Date = g.Key,
                    ExerciseEntries = g.Count(),
                    TotalMinutes = g.Sum(w => w.Duration),
                    RecordedCaloriesBurned = g.Sum(w => w.CaloriesBurned)
                })
                .ToList();

            vm.MeasurementForm = new ClientMeasurementForm
            {
                DateRecorded = DateTime.Today
            };

            return vm;
        }
    }
}
