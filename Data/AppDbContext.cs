using Microsoft.EntityFrameworkCore;
using tsu_absences_api.Models;

namespace tsu_absences_api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Absence> Absences { get; set; }
    public DbSet<Document> Documents { get; set; }
}