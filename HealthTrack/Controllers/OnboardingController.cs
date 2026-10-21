// Owner: Person A (Auth / Profiles / Onboarding)
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using HealthTrack.Data;
using HealthTrack.Filters;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.ViewModels;
using Newtonsoft.Json;

namespace HealthTrack.Controllers
{
    
    [Authorize]
    [AllowIncompleteOnboarding]
    public class OnboardingController : Controller
    {
        private readonly HealthTrackContext db = new HealthTrackContext();

        private static readonly string[] ClientSteps = { "about", "body", "goals", "review" };
        private static readonly string[] TrainerSteps = { "specialisations", "bio", "review" };

        private const int BioMin = 20, BioMax = 500;      // TrainerProfile.Bio is nvarchar(500)
        private const int MinAge = 13, MaxAge = 100;

        private sealed class Account
        {
            public ApplicationUser User;
            public string Role;
            public bool IsClient { get { return Role == Models.Role.Client; } }
            public string[] Steps { get { return IsClient ? ClientSteps : TrainerSteps; } }
            public OptionKind Kind { get { return IsClient ? OptionKind.Goal : OptionKind.Specialisation; } }
        }

        // ================= GET /Onboarding  (or /Onboarding?step=2) =================
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Index(int? step)
        {
            Account acc;
            var stop = Guard(out acc);
            if (stop != null) return stop;

            byte maxStep;
            var data = LoadDraft(acc, out maxStep);
            var s = Math.Max(1, Math.Min(step ?? maxStep, maxStep));   // cannot jump past the furthest unlocked step

            return View(Build(acc, data, s, maxStep));
        }

        // ================= POST /Onboarding  (Continue / Back / Finish) =================
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Index(OnboardingInput input)
        {
            Account acc;
            var stop = Guard(out acc);
            if (stop != null) return stop;

            byte maxStep;
            var data = LoadDraft(acc, out maxStep);
            var steps = acc.Steps;

            // A tampered or stale step number goes back to where the user really is.
            if (input == null || input.Step < 1 || input.Step > maxStep)
                return RedirectToAction("Index");

            var s = input.Step;
            var key = steps[s - 1];
            var goingBack = string.Equals(input.Nav, "back", StringComparison.OrdinalIgnoreCase);

            if (key == "review")
                return goingBack ? RedirectToAction("Index", new { step = s - 1 }) : Finish(acc, data, maxStep);

            Merge(key, input, data);

            if (goingBack)
            {
                // Keep what was typed (even if incomplete) and go back without validating.
                ModelState.Clear();
                SaveDraft(acc, data, maxStep);
                return RedirectToAction("Index", new { step = Math.Max(1, s - 1) });
            }

            ValidateStep(acc, key, data, input.CustomEntry, ModelState);
            if (!ModelState.IsValid)
                return View(Build(acc, data, s, maxStep));

            var unlocked = (byte)Math.Max(maxStep, Math.Min(s + 1, steps.Length));
            SaveDraft(acc, data, unlocked);
            return RedirectToAction("Index", new { step = s + 1 });
        }

        // ================= GET /Onboarding/Done =================
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Done()
        {
            var acc = LoadAccount();
            if (acc == null) return RedirectToAction("Login", "Account");
            if (OnboardingStatus.Check(db, acc.User.UserId, acc.Role) != OnboardingState.Complete)
                return RedirectToAction("Index");

            var vm = new OnboardingDoneViewModel
            {
                Role = acc.Role,
                FirstName = acc.User.FirstName,
                DashboardUrl = acc.IsClient ? Url.Action("Overview", "Client") : Url.Action("Index", "Trainer")
            };
            if (acc.IsClient)
            {
                var cid = db.ClientProfiles.Where(c => c.UserId == acc.User.UserId).Select(c => c.ClientId).First();
                vm.Chips = (from x in db.ClientGoals
                            join g in db.FitnessGoals on x.FitnessGoalId equals g.FitnessGoalId
                            where x.ClientId == cid
                            orderby x.Position
                            select g.Name).ToList();
            }
            else
            {
                var tid = db.TrainerProfiles.Where(t => t.UserId == acc.User.UserId).Select(t => t.TrainerId).First();
                vm.Chips = (from x in db.TrainerSpecialisations
                            join p in db.Specialisations on x.SpecialisationId equals p.SpecialisationId
                            where x.TrainerId == tid
                            orderby x.Position
                            select p.Name).ToList();
            }
            return View(vm);
        }

        // ================= finishing =================
        private ActionResult Finish(Account acc, OnboardingData data, byte maxStep)
        {
            var steps = acc.Steps;

            // 1. Re-validate EVERY step from the stored draft (the draft is never trusted).
            List<OptionPick> picks = null;
            for (var i = 0; i < steps.Length - 1; i++)
            {
                var check = new ModelStateDictionary();
                var result = ValidateStep(acc, steps[i], data, null, check);
                if (result != null) picks = result;
                if (!check.IsValid)
                {
                    TempData["Error"] = "Please check this step before finishing.";
                    return RedirectToAction("Index", new { step = i + 1 });
                }
            }

            // 2. Already finished in another tab? Nothing to do.
            if (OnboardingStatus.Check(db, acc.User.UserId, acc.Role) == OnboardingState.Complete)
                return RedirectToAction("Done");

            // 3. Build profile + links + new options + summary, and remove the draft: ONE SaveChanges.
            var uid = acc.User.UserId;
            if (acc.IsClient)
            {
                var goals = ProfileOptions.GoalEntities(db, picks, uid);
                var profile = new ClientProfile
                {
                    UserId = uid,
                    DateOfBirth = data.DateOfBirth.Value.Date,
                    Gender = data.Gender,
                    Height = data.Height.Value,
                    Weight = data.Weight.Value,
                    Goal = ProfileOptions.Summary(goals.Select(g => g.Name), 200)   // backward-compatible display text
                };
                db.ClientProfiles.Add(profile);
                for (var i = 0; i < goals.Count; i++)
                    db.ClientGoals.Add(new ClientGoal { Client = profile, FitnessGoal = goals[i], Position = i });
            }
            else
            {
                var specs = ProfileOptions.SpecialisationEntities(db, picks, uid);
                var profile = new TrainerProfile
                {
                    UserId = uid,
                    Bio = data.Bio,
                    AverageRating = 0m,
                    Specialisation = ProfileOptions.Summary(specs.Select(x => x.Name), 100)
                };
                db.TrainerProfiles.Add(profile);
                for (var i = 0; i < specs.Count; i++)
                    db.TrainerSpecialisations.Add(new TrainerSpecialisation { Trainer = profile, Specialisation = specs[i], Position = i });
            }

            var draft = db.OnboardingDrafts.Find(uid);
            if (draft != null) db.OnboardingDrafts.Remove(draft);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // e.g. double-click / second tab (unique index on profile UserId) or a custom name
                // created by someone else at the same moment (unique index on Name).
                using (var fresh = new HealthTrackContext())
                {
                    if (OnboardingStatus.Check(fresh, uid, acc.Role) == OnboardingState.Complete)
                        return RedirectToAction("Done");
                }
                TempData["Error"] = "We couldn't save your profile just now. Nothing was lost - please press Finish again.";
                return RedirectToAction("Index", new { step = steps.Length });
            }
            catch (DbEntityValidationException)
            {
                TempData["Error"] = "Some details are not valid. Please check each step and try again.";
                return RedirectToAction("Index", new { step = 1 });
            }

            return RedirectToAction("Done");
        }

        // ================= validation (server side, per step) =================
        // Returns the validated option picks for the goals/specialisations step, otherwise null.
        private List<OptionPick> ValidateStep(Account acc, string key, OnboardingData data, List<string> plainCustom, ModelStateDictionary ms)
        {
            switch (key)
            {
                case "about":
                    if (!data.DateOfBirth.HasValue && ms.IsValidField("DateOfBirth"))
                        ms.AddModelError("DateOfBirth", "Enter your date of birth.");
                    else if (data.DateOfBirth.HasValue)
                    {
                        var dob = data.DateOfBirth.Value.Date;
                        var age = AgeOn(dob, DateTime.Today);
                        if (dob > DateTime.Today) ms.AddModelError("DateOfBirth", "Date of birth can't be in the future.");
                        else if (age < MinAge) ms.AddModelError("DateOfBirth", "You must be at least " + MinAge + " to use HealthTrack.");
                        else if (age > MaxAge) ms.AddModelError("DateOfBirth", "Please check the year of your date of birth.");
                    }
                    if (string.IsNullOrEmpty(data.Gender))
                        ms.AddModelError("Gender", "Choose an option.");
                    else if (!OnboardingViewModel.GenderOptions.Contains(data.Gender))
                        ms.AddModelError("Gender", "Choose one of the listed options.");
                    return null;

                case "body":
                    CheckRange(ms, "Height", data.Height, 50, 260, "height", "cm");   // ClientProfile [Range(50, 260)]
                    CheckRange(ms, "Weight", data.Weight, 20, 500, "weight", "kg");   // ClientProfile [Range(20, 500)]
                    return null;

                case "goals":
                case "specialisations":
                    var linked = new List<int>();
                    var picks = ProfileOptions.Validate(db, acc.Kind, acc.User.UserId, data.Selections, plainCustom, linked, ms, "Selections");
                    // Store the cleaned list (custom names that match an existing option become "id:N").
                    if (ms.IsValidField("Selections")) data.Selections = picks.Select(p => p.Token).ToList();
                    return picks;

                case "bio":
                    var len = (data.Bio ?? "").Length;
                    if (len == 0) ms.AddModelError("Bio", "Write a short bio for your clients.");
                    else if (len < BioMin) ms.AddModelError("Bio", "Write at least " + BioMin + " characters (" + len + " so far).");
                    else if (len > BioMax) ms.AddModelError("Bio", "Keep it under " + BioMax + " characters (" + len + " now).");
                    return null;
            }
            return null;
        }

        private static void CheckRange(ModelStateDictionary ms, string field, decimal? value, decimal min, decimal max, string label, string unit)
        {
            if (!ms.IsValidField(field)) return;   // "abc" etc. already has a binding error
            if (!value.HasValue) { ms.AddModelError(field, "Enter your " + label + "."); return; }
            if (value.Value < min || value.Value > max)
                ms.AddModelError(field, "Enter a " + label + " between " + min + " and " + max + " " + unit + ".");
        }

        private static int AgeOn(DateTime dob, DateTime today)
        {
            var age = today.Year - dob.Year;
            if (dob > today.AddYears(-age)) age--;
            return age;
        }

        // Copy only the fields that belong to this step (other fields in the post are ignored).
        private static void Merge(string key, OnboardingInput input, OnboardingData data)
        {
            switch (key)
            {
                case "about":
                    data.DateOfBirth = input.DateOfBirth.HasValue ? input.DateOfBirth.Value.Date : (DateTime?)null;
                    data.Gender = Clip(Trimmed(input.Gender), 20);
                    break;
                case "body":
                    data.Height = input.Height.HasValue ? Math.Round(input.Height.Value, 2) : (decimal?)null;
                    data.Weight = input.Weight.HasValue ? Math.Round(input.Weight.Value, 2) : (decimal?)null;
                    break;
                case "goals":
                case "specialisations":
                    var tokens = (input.Selections ?? new List<string>())
                        .Concat((input.CustomEntry ?? new List<string>()).Where(c => !string.IsNullOrWhiteSpace(c)).Select(ProfileOptions.NewToken))
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Select(t => Clip(t.Trim(), 60))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(ProfileOptions.MaxSelections * 2)
                        .ToList();
                    data.Selections = tokens;
                    // CustomEntry is now part of Selections; don't add it twice during validation.
                    input.CustomEntry = null;
                    break;
                case "bio":
                    data.Bio = Clip(Trimmed(input.Bio), 2000);
                    break;
            }
        }

        private static string Trimmed(string s) { return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }
        private static string Clip(string s, int max) { return s == null || s.Length <= max ? s : s.Substring(0, max); }

        // ================= view model =================
        private OnboardingViewModel Build(Account acc, OnboardingData data, int step, int maxStep)
        {
            var steps = acc.Steps;
            var key = steps[step - 1];
            var vm = new OnboardingViewModel
            {
                Role = acc.Role,
                FirstName = acc.User.FirstName,
                Step = step,
                MaxStep = maxStep,
                StepKey = key,
                DateOfBirth = data.DateOfBirth,
                Gender = data.Gender,
                Height = data.Height,
                Weight = data.Weight,
                Bio = data.Bio
            };
            for (var i = 0; i < steps.Length; i++)
                vm.Steps.Add(new OnboardingStepInfo { Number = i + 1, Key = steps[i], Title = Title(steps[i]) });

            switch (key)
            {
                case "about":
                    vm.Heading = "Tell us about you.";
                    vm.Intro = "We use this to personalise your plans. Only you and your trainer can see it.";
                    break;
                case "body":
                    vm.Heading = "Your starting point.";
                    vm.Intro = "Your current height and weight. You can track changes later.";
                    break;
                case "goals":
                    vm.Heading = "What are you training for?";
                    vm.Intro = "Pick everything that applies. Your trainer uses this to build your plans.";
                    break;
                case "specialisations":
                    vm.Heading = "What do you coach?";
                    vm.Intro = "Pick your specialisations. Clients see these on your profile.";
                    break;
                case "bio":
                    vm.Heading = "Introduce yourself.";
                    vm.Intro = "A few lines about how you coach and who you work best with.";
                    break;
                default:
                    vm.Heading = "Check and finish.";
                    vm.Intro = "Make sure everything looks right. You can go back to change anything.";
                    break;
            }

            var options = ProfileOptions.Available(db, acc.Kind, acc.User.UserId, ProfileOptions.IdsIn(data.Selections));
            if (key == "goals" || key == "specialisations")
            {
                vm.Picker = new OptionPickerViewModel
                {
                    Id = acc.IsClient ? "goals" : "specs",
                    Label = acc.IsClient ? "Fitness goals" : "Specialisations",
                    Noun = acc.IsClient ? "goal" : "specialisation",
                    Hint = "Choose 1 to " + ProfileOptions.MaxSelections + ". Can't find it? Type it and add your own.",
                    Options = options,
                    SelectedTokens = data.Selections ?? new List<string>()
                };
            }

            if (key == "review")
            {
                var names = NamesFor(data.Selections, options);
                if (acc.IsClient)
                {
                    vm.Review.Add(new ReviewRow { Label = "Date of birth", Value = data.DateOfBirth.HasValue ? data.DateOfBirth.Value.ToString("d MMMM yyyy", CultureInfo.InvariantCulture) : "-", EditStep = 1 });
                    vm.Review.Add(new ReviewRow { Label = "Gender", Value = data.Gender ?? "-", EditStep = 1 });
                    vm.Review.Add(new ReviewRow { Label = "Height", Value = data.Height.HasValue ? data.Height.Value.ToString("0.##", CultureInfo.InvariantCulture) + " cm" : "-", EditStep = 2 });
                    vm.Review.Add(new ReviewRow { Label = "Weight", Value = data.Weight.HasValue ? data.Weight.Value.ToString("0.##", CultureInfo.InvariantCulture) + " kg" : "-", EditStep = 2 });
                    vm.Review.Add(new ReviewRow { Label = "Goals", Chips = names, EditStep = 3 });
                }
                else
                {
                    vm.Review.Add(new ReviewRow { Label = "Specialisations", Chips = names, EditStep = 1 });
                    vm.Review.Add(new ReviewRow { Label = "Bio", Value = data.Bio ?? "-", EditStep = 2 });
                }
            }
            return vm;
        }

        private static string Title(string key)
        {
            switch (key)
            {
                case "about": return "About you";
                case "body": return "Body";
                case "goals": return "Goals";
                case "specialisations": return "Specialisations";
                case "bio": return "Bio";
                default: return "Review";
            }
        }

        private static List<string> NamesFor(IEnumerable<string> tokens, List<OptionItem> options)
        {
            var byId = options.ToDictionary(o => o.Id, o => o.Name);
            var names = new List<string>();
            foreach (var t in tokens ?? Enumerable.Empty<string>())
            {
                int id;
                if (t == null) continue;
                if (t.StartsWith("new:", StringComparison.Ordinal)) names.Add(t.Substring(4));
                else if (t.StartsWith("id:", StringComparison.Ordinal) &&
                         int.TryParse(t.Substring(3), NumberStyles.None, CultureInfo.InvariantCulture, out id) && byId.ContainsKey(id))
                    names.Add(byId[id]);
            }
            return names;
        }

        // ================= account + draft helpers =================
        private Account LoadAccount()
        {
            var email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return null;
            var role = db.Roles.Where(r => r.RoleId == user.RoleId).Select(r => r.RoleName).FirstOrDefault();
            return new Account { User = user, Role = role };
        }

        // Returns a redirect when the user should not be on the onboarding pages, otherwise null.
        private ActionResult Guard(out Account acc)
        {
            acc = LoadAccount();
            if (acc == null || !acc.User.IsActive)
            {
                FormsAuthentication.SignOut();
                return RedirectToAction("Login", "Account");
            }
            if (acc.Role != Models.Role.Client && acc.Role != Models.Role.Trainer)
                return RedirectToAction("Index", "Home");                 // Admin: nothing to onboard
            if (OnboardingStatus.Check(db, acc.User.UserId, acc.Role) == OnboardingState.Complete)
                return acc.IsClient ? RedirectToAction("Overview", "Client") : RedirectToAction("Index", "Trainer");
            return null;
        }

        private OnboardingData LoadDraft(Account acc, out byte maxStep)
        {
            maxStep = 1;
            var draft = db.OnboardingDrafts.Find(acc.User.UserId);
            if (draft == null) return new OnboardingData();

            maxStep = (byte)Math.Max(1, Math.Min((int)draft.CurrentStep, acc.Steps.Length));
            try
            {
                return JsonConvert.DeserializeObject<OnboardingData>(draft.DataJson) ?? new OnboardingData();
            }
            catch (JsonException)
            {
                maxStep = 1;                     // unreadable draft: start again rather than crash
                return new OnboardingData();
            }
        }

        private void SaveDraft(Account acc, OnboardingData data, byte currentStep)
        {
            var draft = db.OnboardingDrafts.Find(acc.User.UserId);
            if (draft == null)
            {
                draft = new OnboardingDraft { UserId = acc.User.UserId };
                db.OnboardingDrafts.Add(draft);
            }
            draft.CurrentStep = currentStep;
            draft.DataJson = JsonConvert.SerializeObject(data);
            draft.UpdatedAt = DateTime.Now;
            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // Two tabs created the draft at the same moment: the other one won, which is fine.
            }
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            // Same friendly handling as AccountController for a form left open too long.
            if (filterContext.Exception is HttpAntiForgeryException)
            {
                filterContext.ExceptionHandled = true;
                TempData["Error"] = "This page had expired, so nothing was saved. Please try again.";
                filterContext.Result = RedirectToAction("Index");
                return;
            }
            base.OnException(filterContext);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
