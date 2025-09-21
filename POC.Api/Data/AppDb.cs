using Microsoft.EntityFrameworkCore;
using POC.Api.Models;

namespace POC.Api.Data
{
    public class AppDb : DbContext
    {
        public AppDb(DbContextOptions<AppDb> options) : base(options) { }
        public DbSet<Todo> Todos => Set<Todo>();
    }
}
