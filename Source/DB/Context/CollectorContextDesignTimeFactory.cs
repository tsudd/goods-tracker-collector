namespace GoodsTracker.DataCollector.DB.Context;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class CollectorContextDesignTimeFactory : IDesignTimeDbContextFactory<CollectorContext>
{
    public CollectorContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<CollectorContext> options =
            new DbContextOptionsBuilder<CollectorContext>().UseNpgsql();

        return new CollectorContext(options.Options);
    }
}