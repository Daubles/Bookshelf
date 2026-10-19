using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bookshelf.Data;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = "";
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(u =>
        {
            u.Property(x => x.FirstName).HasMaxLength(50);
            u.HasIndex(x => x.NormalizedEmail).IsUnique();
        });
    }
}