// Owner: Person A. Written ONCE on day 1; nobody else edits this file.
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using HealthTrack.Models;

namespace HealthTrack.Data
{
    public class HealthTrackContext : DbContext
    {
        // "HealthTrackContext" must match the connection string name in Web.config.
        public HealthTrackContext() : base("HealthTrackContext")
        {
            Database.SetInitializer(new HealthTrackInitializer());
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<TrainerProfile> TrainerProfiles { get; set; }
        public DbSet<ClientProfile> ClientProfiles { get; set; }
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<DietPlan> DietPlans { get; set; }
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }
        public DbSet<MealLog> MealLogs { get; set; }
        public DbSet<HealthMetric> HealthMetrics { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Trainer -> Client -> Plan and Trainer -> Plan would create "multiple cascade paths"
            // in SQL Server. Turning off automatic cascade delete avoids that error. Users are
            // deactivated (IsActive = false) instead of being deleted, so nothing is lost.
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            modelBuilder.Entity<TrainerProfile>().Property(t => t.AverageRating).HasPrecision(3, 2);
            modelBuilder.Entity<ClientProfile>().Property(c => c.Height).HasPrecision(5, 2);
            modelBuilder.Entity<ClientProfile>().Property(c => c.Weight).HasPrecision(6, 2);
            modelBuilder.Entity<WorkoutLog>().Property(w => w.Duration).HasPrecision(6, 2);
            modelBuilder.Entity<WorkoutLog>().Property(w => w.CaloriesBurned).HasPrecision(8, 2);
            modelBuilder.Entity<MealLog>().Property(m => m.Calories).HasPrecision(8, 2);
            modelBuilder.Entity<HealthMetric>().Property(h => h.Weight).HasPrecision(6, 2);
            modelBuilder.Entity<HealthMetric>().Property(h => h.BMI).HasPrecision(5, 2);
            modelBuilder.Entity<HealthMetric>().Property(h => h.BodyFat).HasPrecision(5, 2);

            base.OnModelCreating(modelBuilder);
        }
    }

    /// <summary>Creates the database on first run and seeds the three roles.</summary>
    public class HealthTrackInitializer : CreateDatabaseIfNotExists<HealthTrackContext>
    {
        protected override void Seed(HealthTrackContext context)
        {
            context.Roles.Add(new Role { RoleName = Role.Client });
            context.Roles.Add(new Role { RoleName = Role.Trainer });
            context.Roles.Add(new Role { RoleName = Role.Admin });
            context.SaveChanges();
            base.Seed(context);
        }
    }
}
