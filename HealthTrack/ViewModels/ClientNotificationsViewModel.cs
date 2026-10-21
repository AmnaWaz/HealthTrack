
using System;
using System.Collections.Generic;

namespace HealthTrack.ViewModels
{
    public class ClientNotificationsViewModel
    {
        public string FirstName { get; set; }

        public string Filter { get; set; }

        public int TotalCount { get; set; }

        public int UnreadCount { get; set; }

        public int ReadCount { get; set; }

        public List<ClientNotificationItem> Notifications { get; set; }

        public ClientNotificationsViewModel()
        {
            Filter = "all";
            Notifications = new List<ClientNotificationItem>();
        }
    }

    public class ClientNotificationItem
    {
        public int NotificationId { get; set; }

        public string Message { get; set; }

        public string Type { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public string RelatedPageName
        {
            get
            {
                switch ((Type ?? "").ToLowerInvariant())
                {
                    case "appointment":
                        return "Sessions";

                    case "workout":
                        return "Training";

                    case "meal":
                        return "Nutrition";

                    case "plan":
                        return "Overview";

                    case "request":
                        return "My Trainer";

                    case "feedback":
                        return "My Trainer";

                    default:
                        return "Overview";
                }
            }
        }

        public string RelatedController
        {
            get
            {
                switch ((Type ?? "").ToLowerInvariant())
                {
                    case "appointment":
                        return "ClientSessions";

                    case "workout":
                        return "ClientTraining";

                    case "meal":
                        return "ClientNutrition";

                    case "plan":
                        return "Client";

                    case "request":
                        return "ClientTrainer";

                    case "feedback":
                        return "ClientTrainer";

                    default:
                        return "Client";
                }
            }
        }

        public string RelatedAction
        {
            get
            {
                return RelatedController == "Client" ? "Overview" : "Index";
            }
        }
    }
}
