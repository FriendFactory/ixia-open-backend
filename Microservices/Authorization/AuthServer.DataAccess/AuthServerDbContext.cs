using AuthServer.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.DataAccess;

public class AuthServerDbContext(DbContextOptions<AuthServerDbContext> options) : DbContext(options)
{
    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }
    public virtual DbSet<AspNetUserClaims> AspNetUserClaims { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AspNetUser>().ToTable("AspNetUsers");
        modelBuilder.Entity<AspNetUserClaims>().ToTable("AspNetUserClaims");
    }
}