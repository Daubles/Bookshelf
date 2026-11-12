using Microsoft.EntityFrameworkCore;

namespace Bookshelf.Data.Books;

/// <summary>
/// Database access for works and authors.
/// </summary>
public class BookRepository(AppDbContext db, ILogger<BookRepository> logger)
{
    /// <summary>Loads a work with its authors, or null if it isn't stored.</summary>
    public Task<Work?> GetWorkWithDetailsAsync(string workKey, CancellationToken ct = default) =>
        db.Works
            .Include(w => w.Authors).ThenInclude(wa => wa.Author)
            .FirstOrDefaultAsync(w => w.OpenLibraryKey == workKey, ct);

    /// <summary>Stored authors matching the given Open Library keys, keyed by Open Library key.</summary>
    public Task<Dictionary<string, Author>> GetAuthorsByKeyAsync(IReadOnlyCollection<string> authorKeys, CancellationToken ct = default) =>
        db.Authors
            .Where(a => authorKeys.Contains(a.OpenLibraryKey))
            .ToDictionaryAsync(a => a.OpenLibraryKey, ct);

    public void AddWork(Work work) => db.Works.Add(work);

    public void AddAuthor(Author author) => db.Authors.Add(author);

    /// <summary>
    /// Saves pending changes. Returns false if the save failed (most likely because another request
    /// stored the same rows at the same moment); pending changes are then discarded.
    /// </summary>
    public async Task<bool> TrySaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Saving book data failed; discarding changes");
            db.ChangeTracker.Clear();
            return false;
        }
    }
}
