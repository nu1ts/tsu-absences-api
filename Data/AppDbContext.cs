using Microsoft.EntityFrameworkCore;
using tsu_absences_api.Models;

namespace tsu_absences_api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Absence> Absences { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserRoleMapping> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRoleMapping>()
            .HasKey(ur => new { ur.UserId, ur.Role });
    
        modelBuilder.Entity<UserRoleMapping>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);
    }
}