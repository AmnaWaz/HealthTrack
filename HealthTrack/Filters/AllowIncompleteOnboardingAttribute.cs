// Owner: Person A (Auth / Profiles / Onboarding)
using System;

namespace HealthTrack.Filters
{
    /// <summary>
    /// Marks a controller or action that a signed-in user may open BEFORE finishing onboarding
    /// (login/logout/register and the onboarding pages themselves). Everything else is blocked
    /// by <see cref="OnboardingRequiredAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class AllowIncompleteOnboardingAttribute : Attribute
    {
    }
}
