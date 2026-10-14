using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    // Every trainer-only controller inherits from this. It finds the logged-in trainer once per request.
    [Authorize(Roles = "Trainer")]
    public abstract class TrainerBaseController : Controller
    {
        protected readonly HealthTrackContext db = new HealthTrackContext();

        protected ApplicationUser CurrentUser { get; private set; }
        protected TrainerProfile CurrentTrainer { get; private set; }
        protected int CurrentTrainerId { get { return CurrentTrainer.TrainerId; } }
        protected int CurrentUserId { get { return CurrentUser.UserId; } }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var email = User.Identity.Name;
            var user = db.Set<ApplicationUser>().FirstOrDefault(u => u.Email == email);
            TrainerProfile trainer = null;
            if (user != null)
            {
                var uid = user.UserId;
                trainer = db.Set<TrainerProfile>().FirstOrDefault(t => t.UserId == uid);
            }
            if (user == null || trainer == null)
            {
                filterContext.Result = new HttpNotFoundResult("No trainer profile exists for this account.");
                return;
            }
            CurrentUser = user;
            CurrentTrainer = trainer;
            base.OnActionExecuting(filterContext);
        }

        // ---------- shared helpers ----------
        protected class RawClient
        {
            public int ClientId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Goal { get; set; }
            public decimal Height { get; set; }
            public decimal Weight { get; set; }
            public DateTime DateOfBirth { get; set; }
        }

        protected static ClientRow ToClientRow(RawClient c)
        {
            var today = DateTime.Today;
            var age = today.Year - c.DateOfBirth.Year;
            if (c.DateOfBirth.Date > today.AddYears(-age)) age--;
            var first = string.IsNullOrEmpty(c.FirstName) ? "?" : c.FirstName.Substring(0, 1);
            var last = string.IsNullOrEmpty(c.LastName) ? "" : c.LastName.Substring(0, 1);
            return new ClientRow
            {
                ClientId = c.ClientId,
                Name = c.FirstName + " " + c.LastName,
                Initials = (first + last).ToUpper(),
                Goal = c.Goal,
                Height = c.Height,
                Weight = c.Weight,
                Age = age < 0 ? 0 : age
            };
        }

        // mine = clients assigned to this trainer; otherwise clients that have no trainer yet
        protected List<ClientRow> ClientRows(bool mine)
        {
            var tid = CurrentTrainerId;
            var cq = db.Set<ClientProfile>().AsQueryable();
            cq = mine ? cq.Where(c => c.TrainerId == tid) : cq.Where(c => c.TrainerId == null);

            var raw = (from c in cq
                       join u in db.Set<ApplicationUser>() on c.UserId equals u.UserId
                       orderby u.FirstName, u.LastName
                       select new RawClient
                       {
                           ClientId = c.ClientId,
                           FirstName = u.FirstName,
                           LastName = u.LastName,
                           Goal = c.Goal,
                           Height = (decimal)c.Height,
                           Weight = (decimal)c.Weight,
                           DateOfBirth = c.DateOfBirth
                       }).ToList();

            return raw.Select(ToClientRow).ToList();
        }

        protected List<SelectListItem> MyClientOptions()
        {
            return ClientRows(true)
                .Select(c => new SelectListItem { Value = c.ClientId.ToString(), Text = c.Name })
                .ToList();
        }

        protected Dictionary<int, string> ClientNames(List<int> ids)
        {
            if (ids.Count == 0) return new Dictionary<int, string>();
            return (from c in db.Set<ClientProfile>()
                    join u in db.Set<ApplicationUser>() on c.UserId equals u.UserId
                    where ids.Contains(c.ClientId)
                    select new { c.ClientId, u.FirstName, u.LastName })
                   .ToList()
                   .ToDictionary(x => x.ClientId, x => x.FirstName + " " + x.LastName);
        }

        // checks used by both workout and diet plan forms
        protected bool ValidatePlan(PlanFormViewModel vm)
        {
            if (vm.StartDate.HasValue && vm.EndDate.HasValue && vm.EndDate.Value.Date < vm.StartDate.Value.Date)
                ModelState.AddModelError("EndDate", "End date must be on or after the start date.");

            if (vm.ClientId.HasValue)
            {
                var cid = vm.ClientId.Value;
                var tid = CurrentTrainerId;
                var ok = db.Set<ClientProfile>().Any(c => c.ClientId == cid && c.TrainerId == tid);
                if (!ok) ModelState.AddModelError("ClientId", "You can only assign plans to your own clients.");
            }
            return ModelState.IsValid;
        }

        protected PlanFormViewModel PreparedForm(PlanFormViewModel vm, string kind, string route)
        {
            vm.Kind = kind;
            vm.Route = route;
            vm.Clients = MyClientOptions();
            return vm;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
