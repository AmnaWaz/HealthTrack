
using System;
using System.Linq;
using System.Text.RegularExpressions;
using HealthTrack.Models;

namespace HealthTrack.Services
{
    public class TrainerRequestPriorityResult
    {
        public string SuggestedPriority { get; set; }

        public string Reason { get; set; }

        public bool RequiresUrgentReview { get; set; }

        public bool ShowEmergencyGuidance { get; set; }

        public string AnalysisSource { get; set; }
    }

    public static class TrainerRequestPriorityAnalyzer
    {
        private static readonly string[] EmergencyPatterns =
        {
            @"\b(chest pain|chest pressure|heart attack)\b",
            @"\b(cannot breathe|can't breathe|struggling to breathe|severe breathing difficulty)\b",
            @"\b(unconscious|passed out|fainted|seizure)\b",
            @"\b(severe bleeding|bleeding heavily|won't stop bleeding)\b",
            @"\b(stroke symptoms|face drooping|slurred speech)\b",
            @"\b(suicidal|want to kill myself)\b"
        };

        private static readonly string[] HighPriorityPatterns =
        {
            @"\b(severe pain|intense pain|unbearable pain)\b",
            @"\b(dizziness|dizzy|lightheaded|fainting)\b",
            @"\b(injury|injured|sprain|fracture|broken bone)\b",
            @"\b(allergic reaction|food allergy|allergic to)\b",
            @"\b(swelling|swollen|numbness|numb|tingling)\b",
            @"\b(vomiting|dehydrated|dehydration)\b",
            @"\b(low blood sugar|hypoglycemia|hypoglycaemia)\b",
            @"\b(high blood sugar|hyperglycemia|hyperglycaemia)\b",
            @"\b(bleeding|difficulty breathing|shortness of breath)\b",
            @"\b(panic attack|severe anxiety)\b",
            @"\b(knee pain|back pain|shoulder pain|joint pain)\b"
        };

        public static TrainerRequestPriorityResult Analyze(
            string category,
            string subject,
            string description,
            string clientPriority)
        {
            var text = string.Join(
                " ",
                new[]
                {
                    category ?? "",
                    subject ?? "",
                    description ?? ""
                }
            );

            var emergencyDetected = EmergencyPatterns.Any(pattern =>
                Regex.IsMatch(
                    text,
                    pattern,
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant
                )
            );

            var highPriorityDetected = HighPriorityPatterns.Any(pattern =>
                Regex.IsMatch(
                    text,
                    pattern,
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant
                )
            );

            var clientSelectedHigh =
                string.Equals(
                    clientPriority,
                    TrainerRequest.Priorities.High,
                    StringComparison.OrdinalIgnoreCase
                );

            if (emergencyDetected)
            {
                return new TrainerRequestPriorityResult
                {
                    SuggestedPriority = TrainerRequest.Priorities.High,
                    Reason = "Possible emergency-related language detected. Prompt human review is recommended.",
                    RequiresUrgentReview = true,
                    ShowEmergencyGuidance = true,
                    AnalysisSource = "Rules"
                };
            }

            if (highPriorityDetected)
            {
                return new TrainerRequestPriorityResult
                {
                    SuggestedPriority = TrainerRequest.Priorities.High,
                    Reason = "Potential health or safety concern detected in the request.",
                    RequiresUrgentReview = true,
                    ShowEmergencyGuidance = false,
                    AnalysisSource = "Rules"
                };
            }

            if (clientSelectedHigh)
            {
                return new TrainerRequestPriorityResult
                {
                    SuggestedPriority = TrainerRequest.Priorities.High,
                    Reason = "The client marked this request as high priority.",
                    RequiresUrgentReview = true,
                    ShowEmergencyGuidance = false,
                    AnalysisSource = "Client"
                };
            }

            return new TrainerRequestPriorityResult
            {
                SuggestedPriority = TrainerRequest.Priorities.Normal,
                Reason = "No predefined priority indicators were detected. Human review is still required.",
                RequiresUrgentReview = false,
                ShowEmergencyGuidance = false,
                AnalysisSource = "Rules"
            };
        }
    }
}
