using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Mvc;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    // Opened from the cogwheel at the bottom of the sidebar: Profile + Settings.
    public class ProfileController : TrainerBaseController
    {

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadPhoto(System.Web.HttpPostedFileBase photo)
        {
            string newPath;
            try { newPath = HealthTrack.Services.ProfilePhotoService.Save(photo, Server); }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
            catch (ArgumentException)
            {
                TempData["Error"] = "The selected file is not a valid image.";
                return RedirectToAction("Index");
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                TempData["Error"] = "The selected image could not be processed.";
                return RedirectToAction("Index");
            }
            catch (System.OutOfMemoryException)
            {
                TempData["Error"] = "The selected file is not a valid image.";
                return RedirectToAction("Index");
            }
            var previous = CurrentUser.ProfilePhotoPath;
            CurrentUser.ProfilePhotoPath = newPath;
            try { db.SaveChanges(); }
            catch { HealthTrack.Services.ProfilePhotoService.DeleteOld(newPath, Server); throw; }
            try { HealthTrack.Services.ProfilePhotoService.DeleteOld(previous, Server); } catch (System.IO.IOException) { }
            TempData["Success"] = "Profile photo updated.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemovePhoto()
        {
            var previous = CurrentUser.ProfilePhotoPath;
            CurrentUser.ProfilePhotoPath = null;
            db.SaveChanges();
            try { HealthTrack.Services.ProfilePhotoService.DeleteOld(previous, Server); } catch (System.IO.IOException) { }
            TempData["Success"] = "Profile photo removed.";
            return RedirectToAction("Index");
        }
        // ---------- PROFILE ----------
        public ActionResult Index()
        {
            return View(new ProfileViewModel
            {
                FirstName = CurrentUser.FirstName,
                LastName = CurrentUser.LastName,
                Email = CurrentUser.Email,
                Phone = CurrentUser.Phone,
                Specialisation = GetSpecialisationsForEdit(),
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

            var names = (vm.Specialisation ?? "").Split(',')
                .Select(x => x.Trim()).Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (names.Count > 3) ModelState.AddModelError("Specialisation", "Choose up to three specialisations.");
            if (names.Any(x => x.Length > 50)) ModelState.AddModelError("Specialisation", "Each specialisation must be 50 characters or fewer.");
            if (string.Join(", ", names).Length > 100) ModelState.AddModelError("Specialisation", "Keep the combined specialisation names within 100 characters.");
            if (!ModelState.IsValid) return View(vm);

            using (var tx = db.Database.BeginTransaction())
            {
            CurrentUser.FirstName = vm.FirstName;
            CurrentUser.LastName = vm.LastName;
            CurrentUser.Phone = phone;
            CurrentTrainer.Bio = vm.Bio;
            var tid = CurrentTrainerId;
            var uid = CurrentUserId;
            var existingLinks = db.Set<TrainerSpecialisation>().Where(x => x.TrainerId == tid).ToList();
            foreach (var link in existingLinks) db.Set<TrainerSpecialisation>().Remove(link);
            var position = 0;
            var selected = new List<string>();
            foreach (var name in names)
            {
                var lowerName = name.ToLower();
                var option = db.Set<Specialisation>().FirstOrDefault(x => x.Name.ToLower() == lowerName);
                if (option == null)
                {
                    option = new Specialisation { Name = name, IsPredefined = false, SortOrder = 999,
                        CreatedByUserId = uid, CreatedAt = DateTime.Now };
                    db.Set<Specialisation>().Add(option);
                    db.SaveChanges();
                }
                db.Set<TrainerSpecialisation>().Add(new TrainerSpecialisation
                {
                    TrainerId = tid, SpecialisationId = option.SpecialisationId, Position = position++
                });
                selected.Add(option.Name);
            }
            CurrentTrainer.Specialisation = string.Join(", ", selected);
            db.SaveChanges();
            tx.Commit();
            }

            TempData["Success"] = "Profile saved.";
            return RedirectToAction("Index");
        }

        private string GetSpecialisationsForEdit()
        {
            var tid = CurrentTrainerId;
            var names = (from ts in db.Set<TrainerSpecialisation>()
                         join sp in db.Set<Specialisation>() on ts.SpecialisationId equals sp.SpecialisationId
                         where ts.TrainerId == tid
                         orderby ts.Position
                         select sp.Name).ToList();
            return names.Count > 0 ? string.Join(", ", names) : CurrentTrainer.Specialisation;
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
