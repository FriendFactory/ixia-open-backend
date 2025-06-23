namespace Frever.Shared.MainDb.Entities;

public class BrandAndGenre
{
    public long BrandId { get; set; }
    public long GenreId { get; set; }
    public int BrandSortKey { get; set; }
    public int GenreSortKey { get; set; }

    public virtual Brand Brand { get; set; }
    public virtual Genre Genre { get; set; }
}