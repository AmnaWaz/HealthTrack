using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    [Table("HealthcareAppointments")]
    public class HealthcareAppointment
    {
        [Key] public int HealthcareAppointmentId { get; set; }
        [Required] public int ClientId { get; set; }
        [ForeignKey("ClientId")] public virtual ClientProfile Client { get; set; }
        [Required, StringLength(120)] public string ProfessionalName { get; set; }
        [StringLength(120)] public string Clinic { get; set; }
        [Required] public DateTime AppointmentDateTime { get; set; }
        [Required, StringLength(20)] public string Status { get; set; }
        [StringLength(500)] public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
