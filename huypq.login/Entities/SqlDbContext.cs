using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace huypq.login.Entities
{
    public partial class SqlDbContext : DbContext
    {
        public SqlDbContext(DbContextOptions<SqlDbContext> options) : base(options)
        {
            ChangeTracker.AutoDetectChangesEnabled = false;
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Application>(entity =>
            {
                entity.HasKey(p => p.ID)
                    .HasName("PK_Application");

                entity.HasOne(d => d.UserIDNavigation)
                    .WithMany(p => p.ApplicationUserIDNavigation)
                    .HasForeignKey(d => d.UserID)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Application_User");
            });

            modelBuilder.Entity<RedirectUri>(entity =>
            {
                entity.HasKey(p => p.ID)
                    .HasName("PK_RedirectUri");

                entity.Property(p => p.Uri)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.HasOne(d => d.ApplicationIDNavigation)
                    .WithMany(p => p.RedirectUriApplicationIDNavigation)
                    .HasForeignKey(d => d.ApplicationID)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_RedirectUri_Application");
            });

            modelBuilder.Entity<Scope>(entity =>
            {
                entity.HasKey(p => p.ID)
                    .HasName("PK_Scope");

                entity.Property(p => p.ScopeName)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.HasOne(d => d.ApplicationIDNavigation)
                    .WithMany(p => p.ScopeApplicationIDNavigation)
                    .HasForeignKey(d => d.ApplicationID)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Scope_Application");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(p => p.ID)
                    .HasName("PK_SmtUser");

                entity.Property(p => p.CreateDate).HasColumnType("datetime2(7)");
                entity.Property(p => p.LastLogin).HasColumnType("datetime2(7)");
                entity.Property(p => p.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(p => p.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(128);
            });

            modelBuilder.Entity<UserScope>(entity =>
            {
                entity.HasKey(p => p.ID)
                    .HasName("PK_UserScope");

                entity.HasOne(d => d.ScopeIDNavigation)
                    .WithMany(p => p.UserScopeScopeIDNavigation)
                    .HasForeignKey(d => d.ScopeID)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_UserScope_Scope");

                entity.HasOne(d => d.UserIDNavigation)
                    .WithMany(p => p.UserScopeUserIDNavigation)
                    .HasForeignKey(d => d.UserID)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_UserScope_User");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        public DbSet<Application> Application { get; set; }
        public DbSet<RedirectUri> RedirectUri { get; set; }
        public DbSet<Scope> Scope { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<UserScope> UserScope { get; set; }
    }
}
