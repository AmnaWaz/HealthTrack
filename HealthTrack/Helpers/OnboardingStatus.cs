// Owner: Person A (Auth / Profiles / Onboarding)
using System.Linq;
using HealthTrack.Data;
using HealthTrack.Models;

namespace HealthTrack.Helpers
{
    public enum OnboardingState
    {
        NotRequired,   // not signed in, unknown user, or a role without a profile (Admin)
        Incomplete,    // Client/Trainer account without its profile row yet
        Complete       // profile exists
    }

    /// <summary>
    /// Single rule for "has this account finished onboarding?":
    /// a Client is complete when a ClientProfile exists, a Trainer when a TrainerProfile exists.
    /// Accounts created before this feature always got their profile at registration, so they
    /// are complete automatically and are never sent through onboarding.
    /// The role is read from the database, not from the cookie.
    /// </summary>
    public static class OnboardingStatus
    {
        public static OnboardingState Check(HealthTrackContext db, int userId, string roleName)
        {
            if (roleName == Role.Client)
                return db.ClientProfiles.Any(c => c.UserId == userId) ? OnboardingState.Complete : OnboardingState.Incomplete;
            if (roleName == Role.Trainer)
                return db.TrainerProfiles.Any(t => t.UserId == userId) ? OnboardingState.Complete : OnboardingState.Incomplete;
            return OnboardingState.NotRequired;
        }

        public static OnboardingState ForEmail(HealthTrackContext db, string email)
        {
            if (string.IsNullOrEmpty(email)) return OnboardingState.NotRequired;
            var row = (from u in db.Users
                       join r in db.Roles on u.RoleId equals r.RoleId
                       where u.Email == email
                       select new { u.UserId, r.RoleName }).FirstOrDefault();
            if (row == null) return OnboardingState.NotRequired;
            return Check(db, row.UserId, row.RoleName);
        }
    }
}
