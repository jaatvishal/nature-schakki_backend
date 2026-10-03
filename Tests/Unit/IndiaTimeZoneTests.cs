using API.Serialization;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Tests.Unit;

public class IndiaTimeZoneTests
{
    private static IndiaTimeZoneService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApplicationTimeZone:Id"] = "Asia/Kolkata"
            })
            .Build();
        return new IndiaTimeZoneService(configuration);
    }

    [Fact]
    public void ConvertsUtcToIndiaTime_AndBack()
    {
        var service = CreateService();
        var utc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var india = service.ToDisplayTime(utc);

        Assert.Equal(new DateTime(2026, 1, 1, 5, 30, 0), india);
        Assert.Equal(utc, service.ToUtc(india));
    }

    [Fact]
    public void JsonConverter_TreatsEfUnspecifiedValuesAsUtc()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new UtcDateTimeJsonConverter());
        var efValue = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var json = JsonSerializer.Serialize(efValue, options);

        Assert.EndsWith("Z\"", json);
        Assert.Equal(
            DateTimeKind.Utc,
            JsonSerializer.Deserialize<DateTime>(json, options).Kind);
    }
}
