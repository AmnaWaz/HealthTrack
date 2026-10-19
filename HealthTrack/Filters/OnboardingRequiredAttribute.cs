// Owner: Person A (Auth / Profiles / Onboarding)
using System.Net;
using System.Web.Mvc;
using System.Web.Routing;
using HealthTrack.Data;
using HealthTrack.Helpers;

namespace HealthTrack.Filters
{
    /// <summary>
    /// Global server-side gate (registered in App_Start/FilterConfig.cs).
    /// A signed-in Client or Trainer whose profile does not exist yet is sent to /Onboarding from
    /// every page except the ones marked [AllowIncompleteOnboarding].
    ///
    /// It is an AUTHORIZATION filter, so MVC runs it before any action filter, including
    /// TrainerBaseController.OnActionExecuting (which would otherwise answer 404 for a trainer
    /// with no profile). No other controller needs to change for the gate to work.
    /// Hiding a "Skip" button is not the protection; this filter is.
    /// </summary>
    public sealed class OnboardingRequiredAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext.IsChildAction) return;

            var http = filterContext.HttpContext;
            if (http.User == null || http.User.Identity == null || !http.User.Identity.IsAuthenticated) return;

            var action = filterContext.ActionDescriptor;
            if (action.IsDefined(typeof(AllowIncompleteOnboardingAttribute), true) ||
                action.ControllerDescriptor.IsDefined(typeof(AllowIncompleteOnboardingAttribute), true))
                return;

            OnboardingState state;
            using (var db = new HealthTrackContext())
            {
                state = OnboardingStatus.ForEmail(db, http.User.Identity.Name);
            }
            if (state != OnboardingState.Incomplete) return;

            if (http.Request.IsAjaxRequest())
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Finish setting up your profile first.");
                return;
            }

            filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
            {
                { "controller", "Onboarding" },
                { "action", "Index" },
                { "area", "" }
            });
        }
    }
}
