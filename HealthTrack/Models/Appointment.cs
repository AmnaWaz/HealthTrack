// Owner: Person C (Appointments / Client dashboard / Analytics)
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    public static class AppointmentStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved"; // Legacy confirmed status
        public const string Scheduled = "Scheduled";
        public const string Declined = "Declined";
        public const string Cancelled = "Cancelled";
        public const string Completed = "Completed";
    }

    /// <summary>A booking between one client and one trainer.</summary>
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        public int ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual ClientProfile Client { get; set; }

        public int TrainerId { get; set; }
        [ForeignKey("TrainerId")]
        public virtual TrainerProfile Trainer { get; set; }

        // The ER diagram calls this "DateTime". A property with that name clashes with the
        // System.DateTime type inside this class, so the code uses AppointmentDateTime.
        [DataType(DataType.DateTime)]
        public DateTime AppointmentDateTime { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; }           // see AppointmentStatus

        [StringLength(150)]
        public string Location { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }
    }
}
