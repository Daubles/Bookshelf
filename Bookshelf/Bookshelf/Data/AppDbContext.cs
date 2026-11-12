using Bookshelf.Data.Books;
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
    public DbSet<Work> Works => Set<Work>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<WorkAuthor> WorkAuthors => Set<WorkAuthor>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(u =>
        {
            u.Property(x => x.FirstName).HasMaxLength(50);
            u.HasIndex(x => x.NormalizedEmail).IsUnique();
        });

        builder.Entity<Work>(w =>
        {
            w.Property(x => x.OpenLibraryKey).HasMaxLength(20);
            w.HasIndex(x => x.OpenLibraryKey).IsUnique();
            w.Property(x => x.Title).HasMaxLength(500);
            w.Property(x => x.Subtitle).HasMaxLength(500);
            w.Property(x => x.Description).HasColumnType("text");
            w.Property(x => x.Publisher).HasMaxLength(300);
            w.Property(x => x.SourceEditionKey).HasMaxLength(20);
        });

        builder.Entity<Author>(a =>
        {
            a.Property(x => x.OpenLibraryKey).HasMaxLength(20);
            a.HasIndex(x => x.OpenLibraryKey).IsUnique();
            a.Property(x => x.Name).HasMaxLength(300);
            a.Property(x => x.BirthDate).HasMaxLength(50);
            a.Property(x => x.DeathDate).HasMaxLength(50);
            a.Property(x => x.Bio).HasColumnType("text");
        });

        builder.Entity<WorkAuthor>(wa =>
        {
            wa.HasKey(x => new { x.WorkId, x.AuthorId });

            wa.HasOne(x => x.Work)
                .WithMany(w => w.Authors)
                .HasForeignKey(x => x.WorkId)
                .OnDelete(DeleteBehavior.Cascade);

            wa.HasOne(x => x.Author)
                .WithMany(a => a.Works)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            wa.HasIndex(x => x.AuthorId);
        });
    }
}