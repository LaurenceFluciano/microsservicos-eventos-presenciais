using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Api.Infrastructure.Converters;

public class UtcDateTimeOffsetConverter
    : ValueConverter<DateTimeOffset, long>
{
    public UtcDateTimeOffsetConverter()
        : base(
            value => value.UtcTicks,
            value => new DateTimeOffset(value, TimeSpan.Zero)
        )
    {
    }
}