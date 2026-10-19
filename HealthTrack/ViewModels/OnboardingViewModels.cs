// Owner: Person A (Auth / Profiles / Onboarding)
using System;
using System.Collections.Generic;
using System.Linq;
using HealthTrack.Helpers;

namespace HealthTrack.ViewModels
{
    /// <summary>Data for the reusable searchable multi-select (Views/Shared/_OptionPicker.cshtml).</summary>
    public class OptionPickerViewModel
    {
        public string Id { get; set; } = "picker";                  // unique prefix for element ids
        public string FieldName { get; set; } = "Selections";       // posted tokens ("id:N" / "new:Name")
        public string FallbackFieldName { get; set; } = "CustomEntry"; // no-JavaScript custom boxes
        public string Label { get; set; }                           // e.g. "Fitness goals"
        public string Noun { get; set; }                            // "goal" / "specialisation"
        public string Hint { get; set; }

        public List<OptionItem> Options { get; set; } = new List<OptionItem>();
        public List<string> SelectedTokens { get; set; } = new List<string>();   // in the user's order

        public int Min { get; set; } = ProfileOptions.MinSelections;
        public int Max { get; set; } = ProfileOptions.MaxSelections;
        public int MaxNewCustom { get; set; } = ProfileOptions.MaxNewCustom;

        /// <summary>Custom names chosen but not saved as rows yet (only in a draft) - shown as extra options.</summary>
        public IEnumerable<string> PendingCustomNames
        {
            get
            {
                return SelectedTokens.Where(t => t != null && t.StartsWith("new:", StringComparison.Ordinal))
                                     .Select(t => t.Substring(4));
            }
        }

        public bool IsSelected(string token) { return SelectedTokens.Contains(token); }
    }

    /// <summary>What is stored (as JSON) in OnboardingDrafts.DataJson between steps.</summary>
    public class OnboardingData
    {
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public string Bio { get; set; }
        public List<string> Selections { get; set; } = new List<string>();   // goals (client) or specialisations (trainer)
    }

    /// <summary>Posted by every onboarding step.</summary>
    public class OnboardingInput
    {
        public int Step { get; set; }
        public string Nav { get; set; }             // "next" (default) or "back"

        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public string Bio { get; set; }
        public List<string> Selections { get; set; }
        public List<string> CustomEntry { get; set; }
    }

    public class OnboardingStepInfo
    {
        public int Number { get; set; }
        public string Key { get; set; }
        public string Title { get; set; }
    }

    public class ReviewRow
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public List<string> Chips { get; set; }
        public int EditStep { get; set; }
    }

    public class OnboardingViewModel
    {
        public static readonly string[] GenderOptions = { "Female", "Male", "Other", "Prefer not to say" };

        public string Role { get; set; }            // "Client" / "Trainer"
        public string FirstName { get; set; }

        public int Step { get; set; }
        public int MaxStep { get; set; }            // furthest step unlocked (earlier steps can be revisited)
        public List<OnboardingStepInfo> Steps { get; set; } = new List<OnboardingStepInfo>();
        public string StepKey { get; set; }         // about | body | goals | specialisations | bio | review
        public string Heading { get; set; }
        public string Intro { get; set; }

        public int TotalSteps { get { return Steps.Count; } }
        public bool IsFirst { get { return Step == 1; } }
        public bool IsReview { get { return StepKey == "review"; } }
        public int PercentComplete { get { return TotalSteps == 0 ? 0 : (int)Math.Round(100.0 * Step / TotalSteps); } }

        // fields (names match OnboardingInput so posted values and ModelState errors line up)
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public string Bio { get; set; }
        public OptionPickerViewModel Picker { get; set; }

        public List<ReviewRow> Review { get; set; } = new List<ReviewRow>();

        // date input limits (age 13 - 100)
        public string MinBirthDate { get { return DateTime.Today.AddYears(-100).ToString("yyyy-MM-dd"); } }
        public string MaxBirthDate { get { return DateTime.Today.AddYears(-13).ToString("yyyy-MM-dd"); } }
    }

    public class OnboardingDoneViewModel
    {
        public string Role { get; set; }
        public string FirstName { get; set; }
        public string DashboardUrl { get; set; }
        public List<string> Chips { get; set; } = new List<string>();
    }
}
