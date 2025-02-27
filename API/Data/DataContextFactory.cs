using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace API.Data
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlServer(
                "Server=localhost,1433;Database=DatingDB;User Id=SA;Password=Password@1;TrustServerCertificate=True"
            );

            return new DataContext(optionsBuilder.Options);
        }
    }
}
