using System;
using System.Data;
using System.Data.SqlClient;
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
    [AllowIncompleteOnboarding]
    public class AdminAccessController : Controller
    {
        private readonly HealthTrackContext db = new HealthTrackContext();

        [HttpGet, AllowAnonymous]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Login(string returnUrl)
        {
            if (Request.IsAuthenticated)
                return User.IsInRole(Role.Admin) ? (ActionResult)RedirectToAction("Index", "Admin") : RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View(new AdminLoginViewModel());
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public ActionResult Login(AdminLoginViewModel vm, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(vm);

            var email = vm.Email.Trim();
            var account = db.Users.Include("Role").FirstOrDefault(u => u.Email == email);
            if (account == null || !account.IsActive || account.Role == null || account.Role.RoleName != Role.Admin ||
                !PasswordHelper.Verify(vm.Password, account.PasswordHash))
            {
                ModelState.AddModelError("", "Incorrect admin credentials.");
                return View(vm);
            }

            SignIn(account.Email);
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Admin");
        }

        [HttpGet, AllowAnonymous]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Register()
        {
            if (!FirstAdminRegistrationAvailable()) return HttpNotFound();
            if (Request.IsAuthenticated) return User.IsInRole(Role.Admin) ? (ActionResult)RedirectToAction("Index", "Admin") : RedirectToAction("Index", "Home");
            return View(new AdminRegisterViewModel());
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public ActionResult Register(AdminRegisterViewModel vm)
        {
            if (!FirstAdminRegistrationAvailable()) return HttpNotFound();
            if (Request.IsAuthenticated) return new HttpStatusCodeResult(403);
            if (!ModelState.IsValid) return View(vm);

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    // Serialise first-admin registration across concurrent requests.
                    var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    db.Database.ExecuteSqlCommand(
                        "EXEC @result = sp_getapplock @Resource = 'HealthTrack.FirstAdminRegistration', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000",
                        result);
                    if ((int)result.Value < 0)
                    {
                        ModelState.AddModelError("", "Registration is busy. Please try again.");
                        return View(vm);
                    }

                    var role = db.Roles.FirstOrDefault(r => r.RoleName == Role.Admin);
                    if (role == null)
                    {
                        ModelState.AddModelError("", "Admin role is missing. Run the database setup first.");
                        return View(vm);
                    }
                    if (db.Users.Any(u => u.RoleId == role.RoleId))
                    {
                        transaction.Rollback();
                        return HttpNotFound();
                    }

                    string email = vm.Email.Trim();
                    if (db.Users.Any(u => u.Email == email))
                    {
                        ModelState.AddModelError("Email", "That email is already registered.");
                        return View(vm);
                    }

                    db.Users.Add(new ApplicationUser
                    {
                        FirstName = vm.FirstName.Trim(),
                        LastName = vm.LastName.Trim(),
                        Email = email,
                        Phone = null,
                        PasswordHash = PasswordHelper.Hash(vm.Password),
                        RoleId = role.RoleId,
                        DateJoined = DateTime.UtcNow,
                        IsActive = true
                    });
                    db.SaveChanges();
                    transaction.Commit();
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Unable to create the account. Check the details and try again.");
                    return View(vm);
                }
                catch (SqlException)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Database temporarily unavailable. Please try again.");
                    return View(vm);
                }
            }

            TempData["AdminNotice"] = "Admin account created. Sign in to continue.";
            return RedirectToAction("Login");
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        private bool FirstAdminRegistrationAvailable()
        {
            // The first privileged account can only be bootstrapped from this machine.
            // A hidden path is not an authentication boundary.
            if (!Request.IsLocal) return false;
            var role = db.Roles.FirstOrDefault(r => r.RoleName == Role.Admin);
            return role != null && !db.Users.Any(u => u.RoleId == role.RoleId);
        }

        private void SignIn(string email)
        {
            var now = DateTime.Now;
            var ticket = new FormsAuthenticationTicket(1, email, now, now.AddHours(2), false, Role.Admin);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection
            };
            Response.Cookies.Add(cookie);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
