using System.Data;
using System.Globalization;
using Dapper;

namespace RentAll.Infrastructure.Configuration;

/// <summary>
/// SQL Server calendar columns may surface as <see cref="DateTime"/> or <see cref="DateTimeOffset"/>; Dapper does not
/// map those to <see cref="DateOnly"/> / <see cref="DateOnly?"/> unless these handlers are registered.
/// </summary>
internal static class DapperDateOnlyTypeHandlers
{
    private static int _registered;

    public static void EnsureRegistered()
    {
        if (Interlocked.CompareExchange(ref _registered, 1, 0) != 0)
            return;

        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());
    }

    private sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override DateOnly Parse(object value) => value switch
        {
            DateTime dt => DateOnly.FromDateTime(dt),
            DateTimeOffset dto => DateOnly.FromDateTime(dto.Date),
            string s => DateOnly.Parse(s, CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"Cannot convert {value?.GetType().FullName ?? "null"} to DateOnly.")
        };

        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value = value;
        }
    }

    private sealed class NullableDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly?>
    {
        public override DateOnly? Parse(object value) => value switch
        {
            null or DBNull => null,
            DateTime dt => DateOnly.FromDateTime(dt),
            DateTimeOffset dto => DateOnly.FromDateTime(dto.Date),
            string s => DateOnly.Parse(s, CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().FullName} to DateOnly?.")
        };

        public override void SetValue(IDbDataParameter parameter, DateOnly? value)
        {
            if (!value.HasValue)
            {
                parameter.Value = DBNull.Value;
                return;
            }

            parameter.DbType = DbType.Date;
            parameter.Value = value.Value;
        }
    }
}
