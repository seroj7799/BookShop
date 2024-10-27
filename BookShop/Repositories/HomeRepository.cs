
using Microsoft.EntityFrameworkCore;

namespace BookShop.Repositories
{
    public class HomeRepository : IHomeRepository
    {
        public readonly ApplicationDbContext _db;
        public HomeRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Genre>> Genres()
        {
            return  await _db.Genres.ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetBooks(string sTerm = "", int genreId = 0)
        {
            sTerm = sTerm.ToLower();
            IEnumerable<Book> books = await (from book in _db.Books
                         join genre in _db.Genres
                         on book.GenreId equals genre.Id
                         join stock in _db.Stocks
                         on book.Id equals stock.BookId
                         into stockbook
                         from bookWithStock in stockbook.DefaultIfEmpty()
                         where string.IsNullOrEmpty(sTerm) || (book != null && book.BookName.ToLower().StartsWith(sTerm))
                         select new Book
                         {
                             Id = book.Id,
                             Image = book.Image,
                             AuthorName = book.AuthorName,
                             BookName = book.BookName,
                             Price = book.Price,
                             GenreId = book.GenreId,
                             GenreName = book.GenreName,
                             Quantity = bookWithStock == null ? 0 : bookWithStock.Quantity,
                         }
                         ).ToListAsync();


            if(genreId > 0)
            {
                books = books.Where(a => a.GenreId == genreId).ToList();
            }

            return books;
        }
    }
}
