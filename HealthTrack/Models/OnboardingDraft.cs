// Owner: Person A (Auth / Profiles / Onboarding)
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>
    /// Unfinished onboarding answers for one account, so the user can close the browser and resume.
    /// Deleted in the same SaveChanges that creates the Client/Trainer profile.
    /// The JSON is never trusted: everything is validated again before the profile is created.
    /// </summary>
    [Table("OnboardingDrafts")]
    public class OnboardingDraft
    {
        // Shared primary key: the key IS the user's id (1 account : 0..1 draft, configured in HealthTrackContext).
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }

        public virtual ApplicationUser User { get; set; }

        // Furthest step the user has unlocked (1-based).
        public byte CurrentStep { get; set; }

        [Required]
        public string DataJson { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
