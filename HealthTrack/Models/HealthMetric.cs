// Owner: Person C (Appointments / Client dashboard / Analytics)
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    /// <summary>A body measurement a client records on a given day (used for trend charts).</summary>
    public class HealthMetric
    {
        [Key]
        public int MetricId { get; set; }

        public int ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual ClientProfile Client { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateRecorded { get; set; }

        [Range(20, 500)]
        public decimal Weight { get; set; }          // kg

        public decimal BMI { get; set; }             // calculated: weight / (heightMetres ^ 2)

        [Range(0, 80)]
        public decimal BodyFat { get; set; }         // percent
    }
}
