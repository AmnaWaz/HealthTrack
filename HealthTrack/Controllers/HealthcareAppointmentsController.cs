using System;
using System.Linq;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using HealthTrack.Models;

namespace HealthTrack.Controllers
{
    public class HealthcareAppointmentForm
    {
        [Required, StringLength(120)] public string ProfessionalName { get; set; }
        [StringLength(120)] public string Clinic { get; set; }
        [Required, DataType(DataType.DateTime)] public DateTime? AppointmentDateTime { get; set; }
        [StringLength(500)] public string Notes { get; set; }
    }

    public class HealthcareAppointmentsController : ClientBaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Items = db.Set<HealthcareAppointment>()
                .Where(a => a.ClientId == CurrentClientId)
                .OrderByDescending(a => a.AppointmentDateTime).ToList();
            return View(new HealthcareAppointmentForm { AppointmentDateTime = DateTime.Now.AddDays(1) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(HealthcareAppointmentForm form)
        {
            if (!ModelState.IsValid || !form.AppointmentDateTime.HasValue || form.AppointmentDateTime <= DateTime.Now)
            {
                TempData["Error"] = "Enter the professional's name and a future appointment date.";
                return RedirectToAction("Index");
            }
            db.Set<HealthcareAppointment>().Add(new HealthcareAppointment
            {
                ClientId = CurrentClientId,
                ProfessionalName = form.ProfessionalName.Trim(),
                Clinic = (form.Clinic ?? "").Trim(),
                AppointmentDateTime = form.AppointmentDateTime.Value,
                Status = "Scheduled",
                Notes = (form.Notes ?? "").Trim(),
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();
            TempData["Success"] = "Consultation added to your HealthTrack schedule. This does not book with the clinic.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Cancel(int id)
        {
            var appointment = db.Set<HealthcareAppointment>().FirstOrDefault(a =>
                a.HealthcareAppointmentId == id && a.ClientId == CurrentClientId);
            if (appointment == null) return HttpNotFound();
            if (appointment.Status != "Scheduled" || appointment.AppointmentDateTime <= DateTime.Now)
            {
                TempData["Error"] = "Only upcoming consultations can be removed from your schedule.";
                return RedirectToAction("Index");
            }
            appointment.Status = "Cancelled";
            db.SaveChanges();
            TempData["Success"] = "Consultation removed from your HealthTrack schedule. Contact the clinic separately if you booked there.";
            return RedirectToAction("Index");
        }
    }
}
