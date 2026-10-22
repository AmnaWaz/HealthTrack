using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using HealthTrack.Data;
using HealthTrack.Models;
namespace HealthTrack.Controllers
{
    [Authorize(Roles = "Admin")]
    public abstract class AdminBaseController : Controller
    {
        protected readonly HealthTrackContext db = new HealthTrackContext();
        protected ApplicationUser AdminUser { get; private set; }
        protected override void OnActionExecuting(ActionExecutingContext context)
        {
            var email = User.Identity.Name;
            var user = db.Users.Include("Role").FirstOrDefault(u => u.Email == email);
            if (user == null || !user.IsActive || user.Role == null || user.Role.RoleName != Role.Admin)
            {
                FormsAuthentication.SignOut();
                context.Result = RedirectToAction("Login", "AdminAccess");
                return;
            }
            AdminUser = user;
            ViewBag.AdminName = user.FullName;
            base.OnActionExecuting(context);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
