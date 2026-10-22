using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using HealthTrack.Data;
using HealthTrack.Filters;
using HealthTrack.Helpers;
using HealthTrack.Models;
using HealthTrack.ViewModels;

namespace HealthTrack.Controllers
{
    [AllowIncompleteOnboarding]   // login / logout / register must work before onboarding is finished
    public class AccountController : Controller
    {
        private readonly HealthTrackContext db = new HealthTrackContext();

        // ---------- REGISTER ----------
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Register()
        {
            if (Request.IsAuthenticated) return RedirectByRole(CurrentRole());
            return View(new RegisterViewModel { Role = "Client" });
        }

        // Stage 1 of 2: only the account is created here. The Client/Trainer profile is created by
        // OnboardingController when the mandatory onboarding is finished (see Filters/OnboardingRequiredAttribute).
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel vm)
        {
            if (vm.Role != "Client" && vm.Role != "Trainer")
                ModelState.AddModelError("Role", "Choose Client or Trainer.");

            var phone = PhoneHelper.Normalize(vm.Phone);
            if (phone == null)
                ModelState.AddModelError("Phone", "Enter a valid phone number.");
            else if (phone.Length > 20)                                   // ApplicationUser.Phone is nvarchar(20)
                ModelState.AddModelError("Phone", "That phone number is too long.");

            var users = db.Set<ApplicationUser>();
            if (users.Any(u => u.Email == vm.Email))
                ModelState.AddModelError("Email", "That email is already registered.");
            if (phone != null && users.Any(u => u.Phone == phone))
                ModelState.AddModelError("Phone", "That phone number is already registered.");

            var role = db.Set<Role>().FirstOrDefault(r => r.RoleName == vm.Role);
            if (role == null)
                ModelState.AddModelError("Role", "Role not found in the Roles table.");

            if (!ModelState.IsValid) return View(vm);

            var user = new ApplicationUser
            {
                FirstName = vm.FirstName.Trim(),
                LastName = vm.LastName.Trim(),
                Email = vm.Email.Trim(),
                Phone = phone,
                PasswordHash = PasswordHelper.Hash(vm.Password),
                RoleId = role.RoleId,
                DateJoined = DateTime.UtcNow,
                IsActive = true
            };
            users.Add(user);

            try
            {
                db.SaveChanges();   // one INSERT: either the account exists afterwards or nothing was saved
            }
            catch (DbUpdateException)
            {
                // Someone registered the same email at the same moment (unique index on Email).
                users.Remove(user);
                ModelState.AddModelError("Email", "That email is already registered.");
                return View(vm);
            }

            SignIn(user.Email, role.RoleName, false);
            return RedirectToAction("Index", "Onboarding");
        }

        // ---------- LOGIN (email OR phone + password) ----------
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Login(string returnUrl)
        {
            if (Request.IsAuthenticated) return RedirectByRole(CurrentRole());
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
            if (roleName == Role.Admin)
            {
                ModelState.AddModelError("", "Admin accounts use the private admin sign-in page.");
                return View(vm);
            }

            SignIn(user.Email, roleName, vm.RememberMe);

            
            if (OnboardingStatus.Check(db, user.UserId, roleName) == OnboardingState.Incomplete)
                return RedirectToAction("Index", "Onboarding");

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
        private string CurrentRole()
        {
            if (User.IsInRole("Admin")) return "Admin";
            if (User.IsInRole("Trainer")) return "Trainer";
            if (User.IsInRole("Client")) return "Client";
            return "";
        }

        // A form that was opened while logged out (or in another tab) and submitted after logging in
        // carries an old anti-forgery token. Instead of a crash page, send the user somewhere sensible.
        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.Exception is HttpAntiForgeryException)
            {
                filterContext.ExceptionHandled = true;
                if (Request.IsAuthenticated)
                {
                    filterContext.Result = RedirectByRole(CurrentRole());
                }
                else
                {
                    TempData["Error"] = "Your page expired. Please try again.";
                    filterContext.Result = RedirectToAction("Login");
                }
                return;
            }
            base.OnException(filterContext);
        }

        private ActionResult RedirectByRole(string roleName)
        {
            if (roleName == "Admin") return RedirectToAction("Index", "Admin");
            if (roleName == "Trainer") return RedirectToAction("Index", "Trainer");
            if (roleName == "Client") return RedirectToAction("Overview", "Client");
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