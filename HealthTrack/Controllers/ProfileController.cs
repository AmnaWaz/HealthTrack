using System.Linq;
using System.Web.Mvc;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    // Opened from the cogwheel at the bottom of the sidebar: Profile + Settings.
    public class ProfileController : TrainerBaseController
    {
        // ---------- PROFILE ----------
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

        // ---------- SETTINGS ----------
        // GET: /Profile/Settings   (theme and sidebar options are saved in the browser; the password lives here)
        public ActionResult Settings()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel vm)
        {
            if (!string.IsNullOrEmpty(vm.CurrentPassword) &&
                !PasswordHelper.Verify(vm.CurrentPassword, CurrentUser.PasswordHash))
                ModelState.AddModelError("CurrentPassword", "That is not your current password.");

            if (!string.IsNullOrEmpty(vm.NewPassword) && vm.NewPassword == vm.CurrentPassword)
                ModelState.AddModelError("NewPassword", "Choose a password different from the current one.");

            if (!ModelState.IsValid) return View("Settings", vm);

            CurrentUser.PasswordHash = PasswordHelper.Hash(vm.NewPassword);
            db.SaveChanges();

            TempData["Success"] = "Password changed.";
            return RedirectToAction("Settings");
        }
    }
}
