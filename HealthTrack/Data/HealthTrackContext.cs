// Owner: Person A. Only Person A edits this file (ask before changing it).
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using HealthTrack.Helpers;
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

        // ---- Onboarding (multi-select goals / specialisations + resumable drafts) ----
        public DbSet<FitnessGoal> FitnessGoals { get; set; }
        public DbSet<ClientGoal> ClientGoals { get; set; }
        public DbSet<Specialisation> Specialisations { get; set; }
        public DbSet<TrainerSpecialisation> TrainerSpecialisations { get; set; }
        public DbSet<OnboardingDraft> OnboardingDrafts { get; set; }

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

            // One account has at most one onboarding draft; the draft's key is the user's id.
            modelBuilder.Entity<OnboardingDraft>().HasRequired(d => d.User).WithOptional();

            base.OnModelCreating(modelBuilder);
        }
    }

    /// <summary>
    /// Creates the database on first run and seeds the roles and the predefined goals/specialisations.
    ///
    /// An EXISTING database is left alone: it is upgraded with the scripts in the repository's
    /// Database folder (01_verify_schema.sql, 02_onboarding_update.sql). This replaces
    /// CreateDatabaseIfNotExists, which throws "The model backing the context has changed" on
    /// EF-created databases as soon as a new table is added to the model, and which never seeds
    /// databases that were created from a SQL script.
    /// </summary>
    public class HealthTrackInitializer : IDatabaseInitializer<HealthTrackContext>
    {
        public void InitializeDatabase(HealthTrackContext context)
        {
            if (context.Database.Exists()) return;

            context.Database.Create();
            Seed(context);
        }

        private static void Seed(HealthTrackContext context)
        {
            context.Roles.Add(new Role { RoleName = Role.Client });
            context.Roles.Add(new Role { RoleName = Role.Trainer });
            context.Roles.Add(new Role { RoleName = Role.Admin });

            var now = DateTime.Now;
            var order = 1;
            foreach (var name in ProfileOptions.PredefinedGoals)
                context.FitnessGoals.Add(new FitnessGoal { Name = name, IsPredefined = true, SortOrder = order++, CreatedAt = now });

            order = 1;
            foreach (var name in ProfileOptions.PredefinedSpecialisations)
                context.Specialisations.Add(new Specialisation { Name = name, IsPredefined = true, SortOrder = order++, CreatedAt = now });

            context.SaveChanges();
        }
    }
}
