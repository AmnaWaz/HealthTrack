using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class WorkoutPlansController : TrainerBaseController
    {
        private const string Kind = "Workout";
        private const string Route = "WorkoutPlans";
        private const string IndexView = "~/Views/Plans/Index.cshtml";
        private const string FormView = "~/Views/Plans/Form.cshtml";

        // GET: /WorkoutPlans
        public ActionResult Index()
        {
            var tid = CurrentTrainerId;
            var today = DateTime.Today;

            var raw = db.Set<WorkoutPlan>()
                .Where(p => p.TrainerId == tid)
                .OrderByDescending(p => p.StartDate)
                .Select(p => new { Id = p.WorkoutPlanId, p.PlanName, p.Description, p.StartDate, p.EndDate, p.ClientId })
                .ToList();

            var clientIds = raw.Select(r => (int?)r.ClientId)
                               .Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList();
            var names = ClientNames(clientIds);

            var vm = new PlanIndexViewModel { Kind = Kind, Route = Route };
            vm.Plans = raw.Select(r =>
            {
                int? cid = r.ClientId;
                DateTime? end = r.EndDate;
                return new PlanRow
                {
                    Id = r.Id,
                    PlanName = r.PlanName,
                    Description = r.Description,
                    StartDate = r.StartDate,
                    EndDate = end,
                    ClientName = (cid.HasValue && names.ContainsKey(cid.Value)) ? names[cid.Value] : "Unassigned",
                    Status = PlanStatusHelper.Of(r.StartDate, end, today)
                };
            }).ToList();

            return View(IndexView, vm);
        }

        // GET: /WorkoutPlans/Create
        public ActionResult Create()
        {
            var vm = PreparedForm(new PlanFormViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(28)
            }, Kind, Route);
            return View(FormView, vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(PlanFormViewModel vm)
        {
            PreparedForm(vm, Kind, Route);
            if (!ValidatePlan(vm)) return View(FormView, vm);

            db.Set<WorkoutPlan>().Add(new WorkoutPlan
            {
                PlanName = vm.PlanName,
                Description = vm.Description,
                StartDate = vm.StartDate.Value,
                EndDate = vm.EndDate.Value,
                TrainerId = CurrentTrainerId,
                ClientId = vm.ClientId.Value
            });
            Notify(vm.ClientId.Value, "Your trainer assigned a new workout plan: " + vm.PlanName + ". Open your plans to view it.", Notification.WorkoutReminder);
            db.SaveChanges();

            TempData["Success"] = Kind + " plan created.";
            return RedirectToAction("Index");
        }

        // GET: /WorkoutPlans/Edit/5
        public ActionResult Edit(int id)
        {
            var plan = db.Set<WorkoutPlan>().Find(id);
            if (plan == null || plan.TrainerId != CurrentTrainerId) return HttpNotFound();
            if (!IsMyClient(plan.ClientId))
            {
                TempData["Error"] = "This plan belongs to a former client and is retained as history. You cannot change or delete it.";
                return RedirectToAction("Index");
            }

            var vm = PreparedForm(new PlanFormViewModel
            {
                Id = id,
                PlanName = plan.PlanName,
                Description = plan.Description,
                StartDate = plan.StartDate,
                EndDate = plan.EndDate,
                ClientId = plan.ClientId
            }, Kind, Route);
            return View(FormView, vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(PlanFormViewModel vm)
        {
            var plan = db.Set<WorkoutPlan>().Find(vm.Id);
            if (plan == null || plan.TrainerId != CurrentTrainerId) return HttpNotFound();
            if (!IsMyClient(plan.ClientId))
            {
                TempData["Error"] = "This plan belongs to a former client and is retained as history. You cannot change or delete it.";
                return RedirectToAction("Index");
            }

            PreparedForm(vm, Kind, Route);
            if (!ValidatePlan(vm)) return View(FormView, vm);

            var previousClientId = plan.ClientId;
            var previousName = plan.PlanName;
            plan.PlanName = vm.PlanName;
            plan.Description = vm.Description;
            plan.StartDate = vm.StartDate.Value;
            plan.EndDate = vm.EndDate.Value;
            plan.ClientId = vm.ClientId.Value;
            if (previousClientId != vm.ClientId.Value)
            {
                if (IsMyClient(previousClientId))
                    Notify(previousClientId, "Your trainer removed your assignment to the workout plan: " + previousName + ".", Notification.WorkoutReminder);
                Notify(vm.ClientId.Value, "Your trainer assigned you the workout plan: " + vm.PlanName + ".", Notification.WorkoutReminder);
            }
            else
            {
                Notify(vm.ClientId.Value, "Your trainer updated your workout plan: " + vm.PlanName + ".", Notification.WorkoutReminder);
            }
            db.SaveChanges();

            TempData["Success"] = Kind + " plan updated.";
            return RedirectToAction("Index");
        }

        // POST: /WorkoutPlans/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var plan = db.Set<WorkoutPlan>().Find(id);
            if (plan == null || plan.TrainerId != CurrentTrainerId) return HttpNotFound();
            if (!IsMyClient(plan.ClientId))
            {
                TempData["Error"] = "This plan belongs to a former client and is retained as history. You cannot change or delete it.";
                return RedirectToAction("Index");
            }

            try
            {
                db.Set<WorkoutPlan>().Remove(plan);
                if (IsMyClient(plan.ClientId))
                    Notify(plan.ClientId, "Your trainer deleted the workout plan: " + plan.PlanName + ".", Notification.WorkoutReminder);
                db.SaveChanges();
                TempData["Success"] = Kind + " plan deleted.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "This plan can't be deleted because logs are linked to it.";
            }
            return RedirectToAction("Index");
        }
    }
}
