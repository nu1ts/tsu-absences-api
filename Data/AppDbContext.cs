using Microsoft.EntityFrameworkCore;
using tsu_absences_api.Models;

namespace tsu_absences_api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
                {
                    entity.Property(u => u.Id)
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("gen_random_uuid()");

                    entity.HasIndex(u => u.Email)
                        .IsUnique();
                });

                modelBuilder.Entity<UserRole>(entity =>
                {
                    entity.HasKey(ur => new { ur.UserId, ur.Role });

                    entity.HasOne(ur => ur.User)
                        .WithMany(u => u.Roles)
                        .HasForeignKey(ur => ur.UserId)
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}