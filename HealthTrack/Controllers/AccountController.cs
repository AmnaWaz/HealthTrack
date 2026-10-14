using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using HealthTrack.Data;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    public class AccountController : Controller
    {
        private readonly HealthTrackContext db = new HealthTrackContext();

        // ---------- REGISTER ----------
        [HttpGet]
        public ActionResult Register()
        {
            return View(new RegisterViewModel { Role = "Client" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel vm)
        {
            if (vm.Role != "Client" && vm.Role != "Trainer")
                ModelState.AddModelError("Role", "Choose Client or Trainer.");
            if (vm.Role == "Client" && vm.DateOfBirth == null)
                ModelState.AddModelError("DateOfBirth", "Date of birth is required for clients.");

            var phone = PhoneHelper.Normalize(vm.Phone);
            if (phone == null)
                ModelState.AddModelError("Phone", "Enter a valid phone number.");

            var users = db.Set<ApplicationUser>();
            if (users.Any(u => u.Email == vm.Email))
                ModelState.AddModelError("Email", "That email is already registered.");
            if (phone != null && users.Any(u => u.Phone == phone))
                ModelState.AddModelError("Phone", "That phone number is already registered.");

            var role = db.Set<Role>().FirstOrDefault(r => r.RoleName == vm.Role);
            if (role == null)
                ModelState.AddModelError("Role", "Role not found in the Roles table.");

            if (!ModelState.IsValid) return View(vm);

            using (var tx = db.Database.BeginTransaction())
            {
                var user = new ApplicationUser
                {
                    FirstName = vm.FirstName,
                    LastName = vm.LastName,
                    Email = vm.Email,
                    Phone = phone,
                    PasswordHash = PasswordHelper.Hash(vm.Password),
                    RoleId = role.RoleId,
                    DateJoined = DateTime.UtcNow,
                    IsActive = true
                };
                users.Add(user);
                db.SaveChanges();                       // user.UserId is now filled in

                if (vm.Role == "Client")
                {
                    db.Set<ClientProfile>().Add(new ClientProfile
                    {
                        UserId = user.UserId,
                        DateOfBirth = vm.DateOfBirth.Value,
                        Gender = vm.Gender,
                        Height = vm.Height ?? 0,
                        Weight = vm.Weight ?? 0,
                        Goal = vm.Goal
                    });
                }
                else
                {
                    db.Set<TrainerProfile>().Add(new TrainerProfile
                    {
                        UserId = user.UserId,
                        Specialisation = vm.Specialisation,
                        Bio = vm.Bio
                    });
                }
                db.SaveChanges();
                tx.Commit();

                SignIn(user.Email, role.RoleName, false);
            }
            return RedirectByRole(vm.Role);
        }

        // ---------- LOGIN (email OR phone + password) ----------
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel { Method = "Email" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel vm, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            var byPhone = vm.Method == "Phone";
            string phone = null;

            if (byPhone)
            {
                phone = PhoneHelper.Normalize(vm.Phone);
                if (phone == null) ModelState.AddModelError("Phone", "Enter your phone number.");
            }
            else if (string.IsNullOrWhiteSpace(vm.Email))
            {
                ModelState.AddModelError("Email", "Enter your email.");
            }

            if (!ModelState.IsValid) return View(vm);

            var users = db.Set<ApplicationUser>();
            var user = byPhone
                ? users.FirstOrDefault(u => u.Phone == phone)
                : users.FirstOrDefault(u => u.Email == vm.Email);

            if (user == null || !user.IsActive || !PasswordHelper.Verify(vm.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid " + (byPhone ? "phone number" : "email") + " or password.");
                return View(vm);
            }

            var roleName = db.Set<Role>().Where(r => r.RoleId == user.RoleId).Select(r => r.RoleName).First();
            SignIn(user.Email, roleName, vm.RememberMe);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectByRole(roleName);
        }

        // ---------- LOGOUT ----------
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        // ---------- helpers ----------
        private ActionResult RedirectByRole(string roleName)
        {
            if (roleName == "Trainer") return RedirectToAction("Index", "Trainer");
            if (roleName == "Client") return RedirectToAction("Plans", "Client");
            return RedirectToAction("Index", "Home");
        }

        private void SignIn(string email, string roleName, bool persistent)
        {
            var now = DateTime.Now;
            var ticket = new FormsAuthenticationTicket(
                1, email, now, persistent ? now.AddDays(14) : now.AddHours(2), persistent, roleName);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true
            };
            if (persistent) cookie.Expires = ticket.Expiration;
            Response.Cookies.Add(cookie);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
