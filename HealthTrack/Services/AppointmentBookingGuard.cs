using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using HealthTrack.Data;

namespace HealthTrack.Services
{
    public static class AppointmentBookingGuard
    {
        public static void Lock(HealthTrackContext db, int clientId, int trainerId)
        {
            Acquire(db, "HealthTrack:Booking:Client:" + clientId);
            Acquire(db, "HealthTrack:Booking:Trainer:" + trainerId);
        }

        private static void Acquire(HealthTrackContext db, string key)
        {
            var code = db.Database.SqlQuery<int>(
                "DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource = @p0, " +
                "@LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; SELECT @r;",
                key).Single();
            if (code < 0)
                throw new InvalidOperationException("The appointment booking is busy. Please try again.");
        }
    }
}
