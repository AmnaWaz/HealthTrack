using System.Linq;
using System.Web.Mvc;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    // The settings link at the bottom of the sidebar.
    public class ProfileController : TrainerBaseController
    {
        public ActionResult Index()
        {
            return View(new ProfileViewModel
            {
                FirstName = CurrentUser.FirstName,
                LastName = CurrentUser.LastName,
                Email = CurrentUser.Email,
                Phone = CurrentUser.Phone,
                Specialisation = CurrentTrainer.Specialisation,
                Bio = CurrentTrainer.Bio
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Index(ProfileViewModel vm)
        {
            vm.Email = CurrentUser.Email;

            var phone = PhoneHelper.Normalize(vm.Phone);
            if (phone == null)
                ModelState.AddModelError("Phone", "Enter a valid phone number.");
            else
            {
                var uid = CurrentUserId;
                if (db.Set<ApplicationUser>().Any(u => u.Phone == phone && u.UserId != uid))
                    ModelState.AddModelError("Phone", "That phone number is already registered.");
            }

            if (!ModelState.IsValid) return View(vm);

            CurrentUser.FirstName = vm.FirstName;
            CurrentUser.LastName = vm.LastName;
            CurrentUser.Phone = phone;
            CurrentTrainer.Specialisation = vm.Specialisation;
            CurrentTrainer.Bio = vm.Bio;
            db.SaveChanges();

            TempData["Success"] = "Profile saved.";
            return RedirectToAction("Index");
        }
    }
}
