
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using HealthTrack.Helpers;
using HealthTrack.Models;

namespace HealthTrack.Data
{
    public class HealthTrackContext : DbContext
    {
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
        public DbSet<TrainerRequest> TrainerRequests { get; set; }
        public DbSet<TrainerSelectionRequest> TrainerSelectionRequests { get; set; }

        public DbSet<FitnessGoal> FitnessGoals { get; set; }
        public DbSet<ClientGoal> ClientGoals { get; set; }
        public DbSet<Specialisation> Specialisations { get; set; }
        public DbSet<TrainerSpecialisation> TrainerSpecialisations { get; set; }
        public DbSet<OnboardingDraft> OnboardingDrafts { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            modelBuilder.Entity<TrainerProfile>()
                .Property(t => t.AverageRating)
                .HasPrecision(3, 2);

            modelBuilder.Entity<ClientProfile>()
                .Property(c => c.Height)
                .HasPrecision(5, 2);

            modelBuilder.Entity<ClientProfile>()
                .Property(c => c.Weight)
                .HasPrecision(6, 2);

            modelBuilder.Entity<WorkoutLog>()
                .Property(w => w.Duration)
                .HasPrecision(6, 2);

            modelBuilder.Entity<WorkoutLog>()
                .Property(w => w.CaloriesBurned)
                .HasPrecision(8, 2);

            modelBuilder.Entity<MealLog>()
                .Property(m => m.Calories)
                .HasPrecision(8, 2);

            modelBuilder.Entity<HealthMetric>()
                .Property(h => h.Weight)
                .HasPrecision(6, 2);

            modelBuilder.Entity<HealthMetric>()
                .Property(h => h.BMI)
                .HasPrecision(5, 2);

            modelBuilder.Entity<HealthMetric>()
                .Property(h => h.BodyFat)
                .HasPrecision(5, 2);

            modelBuilder.Entity<OnboardingDraft>()
                .HasRequired(d => d.User)
                .WithOptional();

            base.OnModelCreating(modelBuilder);
        }
    }

    public class HealthTrackInitializer : IDatabaseInitializer<HealthTrackContext>
    {
        public void InitializeDatabase(HealthTrackContext context)
        {
            if (context.Database.Exists())
            {
                return;
            }

            context.Database.Create();
            Seed(context);
        }

        private static void Seed(HealthTrackContext context)
        {
            context.Roles.Add(new Role
            {
                RoleName = Role.Client
            });

            context.Roles.Add(new Role
            {
                RoleName = Role.Trainer
            });

            context.Roles.Add(new Role
            {
                RoleName = Role.Admin
            });

            var now = DateTime.Now;
            var order = 1;

            foreach (var name in ProfileOptions.PredefinedGoals)
            {
                context.FitnessGoals.Add(new FitnessGoal
                {
                    Name = name,
                    IsPredefined = true,
                    SortOrder = order++,
                    CreatedAt = now
                });
            }

            order = 1;

            foreach (var name in ProfileOptions.PredefinedSpecialisations)
            {
                context.Specialisations.Add(new Specialisation
                {
                    Name = name,
                    IsPredefined = true,
                    SortOrder = order++,
                    CreatedAt = now
                });
            }

            context.SaveChanges();
        }
    }
}
