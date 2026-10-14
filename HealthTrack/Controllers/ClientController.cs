using System;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Data;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientController : Controller
    {
        private readonly HealthTrackContext db = new HealthTrackContext();

        // GET: /Client/Plans   (workout + diet plans assigned to the logged-in client)
        public ActionResult Plans()
        {
            var email = User.Identity.Name;
            var user = db.Set<ApplicationUser>().FirstOrDefault(x => x.Email == email);
            if (user == null) return HttpNotFound("User not found.");
            var userId = user.UserId;

            var client = db.Set<ClientProfile>().FirstOrDefault(c => c.UserId == userId);
            if (client == null) return HttpNotFound("No client profile exists for this account.");
            var cid = client.ClientId;
            var today = DateTime.Today;

            var workoutRaw = (from p in db.Set<WorkoutPlan>()
                              join t in db.Set<TrainerProfile>() on p.TrainerId equals t.TrainerId
                              join u in db.Set<ApplicationUser>() on t.UserId equals u.UserId
                              where p.ClientId == cid
                              orderby p.StartDate descending
                              select new { Id = p.WorkoutPlanId, p.PlanName, p.Description, p.StartDate, p.EndDate, u.FirstName, u.LastName })
                             .ToList();

            var dietRaw = (from p in db.Set<DietPlan>()
                           join t in db.Set<TrainerProfile>() on p.TrainerId equals t.TrainerId
                           join u in db.Set<ApplicationUser>() on t.UserId equals u.UserId
                           where p.ClientId == cid
                           orderby p.StartDate descending
                           select new { Id = p.DietPlanId, p.PlanName, p.Description, p.StartDate, p.EndDate, u.FirstName, u.LastName })
                          .ToList();

            var vm = new ClientPlansViewModel { FirstName = user.FirstName };

            vm.Workout = workoutRaw.Select(r =>
            {
                DateTime? end = r.EndDate;
                return new PlanRow
                {
                    Id = r.Id, PlanName = r.PlanName, Description = r.Description,
                    StartDate = r.StartDate, EndDate = end,
                    TrainerName = r.FirstName + " " + r.LastName,
                    Status = PlanStatusHelper.Of(r.StartDate, end, today)
                };
            }).ToList();

            vm.Diet = dietRaw.Select(r =>
            {
                DateTime? end = r.EndDate;
                return new PlanRow
                {
                    Id = r.Id, PlanName = r.PlanName, Description = r.Description,
                    StartDate = r.StartDate, EndDate = end,
                    TrainerName = r.FirstName + " " + r.LastName,
                    Status = PlanStatusHelper.Of(r.StartDate, end, today)
                };
            }).ToList();

            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
