namespace GoodsTracker.DataCollector.DB.Helpers;

using System.Reflection;
using System.Text;

using CsvHelper;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal static class SeedHelper
{
    public static EntityTypeBuilder<TEntity> SeedData<TEntity>(
        this EntityTypeBuilder<TEntity> builder, string recourseName)
        where TEntity : class
    {
        var assembly = Assembly.GetExecutingAssembly();

        using Stream? stream = assembly.GetManifestResourceStream(
            assembly.GetName()
                    .Name +
            ".Data." +
            recourseName);

        if (stream == null)
        {
            return builder;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var csvReader = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
        builder.HasData(csvReader.GetRecords<TEntity>());

        return builder;
    }
}
