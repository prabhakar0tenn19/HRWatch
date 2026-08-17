using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HRWatch.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer("Server=IN-PRABHAKAR-LA;Database=HRWatch;User ID=sa;Password=Cyber1234;Integrated Security=false;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
