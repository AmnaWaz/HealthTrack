
using System;
using System.Linq;
using System.Web.Mvc;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class ClientProfileController : ClientBaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View(BuildProfile());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveProfile(ClientProfileEditForm form)
        {
            var phone = PhoneHelper.Normalize(form.Phone);

            if (!string.IsNullOrWhiteSpace(form.Phone) &&
                phone == null)
            {
                ModelState.AddModelError(
                    "Phone",
                    "Enter a valid phone number."
                );
            }

            if (phone != null && phone.Length > 20)
            {
                ModelState.AddModelError(
                    "Phone",
                    "The phone number is too long."
                );
            }

            if (phone != null &&
                !phone.Any(char.IsDigit))
            {
                ModelState.AddModelError(
                    "Phone",
                    "Enter a valid phone number."
                );
            }

            if (phone != null)
            {
                var userId = CurrentUserId;

                var alreadyUsed = db.Set<ApplicationUser>()
                    .Any(u =>
                        u.Phone == phone &&
                        u.UserId != userId);

                if (alreadyUsed)
                {
                    ModelState.AddModelError(
                        "Phone",
                        "This phone number is already registered."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                var vm = BuildProfile();
                vm.EditForm = form;

                return View("Index", vm);
            }

            CurrentUser.FirstName = form.FirstName.Trim();
            CurrentUser.LastName = form.LastName.Trim();
            CurrentUser.Phone = phone;

            var heightChanged = CurrentClient.Height != form.Height;

            CurrentClient.Height = form.Height;

            if (heightChanged)
            {
                var heightMetres = form.Height / 100m;

                var measurements = db.Set<HealthMetric>()
                    .Where(m => m.ClientId == CurrentClientId)
                    .ToList();

                foreach (var measurement in measurements)
                {
                    measurement.BMI = Math.Round(
                        measurement.Weight / (heightMetres * heightMetres),
                        2,
                        MidpointRounding.AwayFromZero
                    );
                }
            }

            db.SaveChanges();

            TempData["Success"] = "Profile updated successfully.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Settings()
        {
            return View(BuildSettings());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ClientPasswordChangeForm form)
        {
            if (!string.IsNullOrWhiteSpace(form.CurrentPassword) &&
                !PasswordHelper.Verify(
                    form.CurrentPassword,
                    CurrentUser.PasswordHash))
            {
                ModelState.AddModelError(
                    "CurrentPassword",
                    "Your current password is incorrect."
                );
            }

            if (!string.IsNullOrWhiteSpace(form.NewPassword) &&
                !string.IsNullOrWhiteSpace(form.CurrentPassword) &&
                form.NewPassword == form.CurrentPassword)
            {
                ModelState.AddModelError(
                    "NewPassword",
                    "Choose a password different from your current password."
                );
            }

            if (!ModelState.IsValid)
            {
                var vm = BuildSettings();
                vm.PasswordForm = form;

                return View("Settings", vm);
            }

            CurrentUser.PasswordHash = PasswordHelper.Hash(
                form.NewPassword
            );

            db.SaveChanges();

            TempData["Success"] = "Password changed successfully.";

            return RedirectToAction("Settings");
        }

        private ClientProfileViewModel BuildProfile()
        {
            var clientId = CurrentClientId;

            var trainerName = "";

            if (CurrentClient.TrainerId.HasValue)
            {
                var trainerId = CurrentClient.TrainerId.Value;

                trainerName = (
                    from t in db.Set<TrainerProfile>()
                    join u in db.Set<ApplicationUser>()
                        on t.UserId equals u.UserId
                    where t.TrainerId == trainerId
                    select u.FirstName + " " + u.LastName
                ).FirstOrDefault();
            }

            var goals = (
                from cg in db.Set<ClientGoal>()
                join fg in db.Set<FitnessGoal>()
                    on cg.FitnessGoalId equals fg.FitnessGoalId
                where cg.ClientId == clientId
                orderby cg.Position
                select new ClientProfileGoalItem
                {
                    FitnessGoalId = fg.FitnessGoalId,
                    Name = fg.Name,
                    Position = cg.Position
                }
            ).ToList();

            var latestMetric = db.Set<HealthMetric>()
                .Where(m => m.ClientId == clientId)
                .OrderByDescending(m => m.DateRecorded)
                .ThenByDescending(m => m.MetricId)
                .FirstOrDefault();

            var currentWeight = latestMetric != null
                ? latestMetric.Weight
                : CurrentClient.Weight;

            var vm = new ClientProfileViewModel
            {
                ClientId = clientId,
                FirstName = CurrentUser.FirstName,
                LastName = CurrentUser.LastName,
                FullName = CurrentUser.FullName,
                Email = CurrentUser.Email,
                Phone = CurrentUser.Phone,
                DateJoined = CurrentUser.DateJoined,
                DateOfBirth = CurrentClient.DateOfBirth,
                Gender = CurrentClient.Gender,
                Height = CurrentClient.Height,
                Weight = currentWeight,
                HasAssignedTrainer = CurrentClient.TrainerId.HasValue,
                TrainerName = trainerName,
                FitnessGoals = goals,
                EditForm = new ClientProfileEditForm
                {
                    FirstName = CurrentUser.FirstName,
                    LastName = CurrentUser.LastName,
                    Phone = CurrentUser.Phone,
                    Height = CurrentClient.Height
                }
            };

            return vm;
        }

        private ClientSettingsViewModel BuildSettings()
        {
            return new ClientSettingsViewModel
            {
                FullName = CurrentUser.FullName,
                Email = CurrentUser.Email,
                PasswordForm = new ClientPasswordChangeForm()
            };
        }
    }
}
