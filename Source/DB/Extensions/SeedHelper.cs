namespace GoodsTracker.DataCollector.DB.Extensions;

using System.Reflection;
using System.Text;

using CsvHelper;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

public static class SeedHelper
{
    public static EntityTypeBuilder<TEntity> SeedData<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string recourceName) where TEntity : class
    {
        var assembly = Assembly.GetExecutingAssembly();

        using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Data." + recourceName))
        {
            if (stream == null)
            {
                return builder;
            }
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                var csvReader = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
                builder.HasData(csvReader.GetRecords<TEntity>());
            }
        }

        return builder;
    }
}