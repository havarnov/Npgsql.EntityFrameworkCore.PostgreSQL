using System;
using System.IO;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Utilities;
using NpgsqlTypes;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping
{
    /// <summary>
    /// A mapping for an arbitrary user POCO to PostgreSQL jsonb or json.
    /// For mapping to .NET string, see <see cref="NpgsqlStringTypeMapping"/>.
    /// </summary>
    public class NpgsqlJsonTypeMapping : NpgsqlTypeMapping
    {
        public NpgsqlJsonTypeMapping([NotNull] string storeType, [NotNull] Type clrType)
            : base(storeType, clrType, storeType == "jsonb" ? NpgsqlDbType.Jsonb : NpgsqlDbType.Json)
        {
            if (storeType != "json" && storeType != "jsonb")
                throw new ArgumentException($"{nameof(storeType)} must be 'json' or 'jsonb'", nameof(storeType));
        }

        protected NpgsqlJsonTypeMapping(RelationalTypeMappingParameters parameters, NpgsqlDbType npgsqlDbType)
            : base(parameters, npgsqlDbType)
        {
        }

        public bool IsJsonb => StoreType == "jsonb";

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new NpgsqlJsonTypeMapping(parameters, NpgsqlDbType);

        protected virtual string EscapeSqlLiteral([NotNull] string literal)
            => Check.NotNull(literal, nameof(literal)).Replace("'", "''");

        protected override string GenerateNonNullSqlLiteral(object value)
        {
            switch (value)
            {
            case JsonDocument _:
            case JsonElement _:
            {
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                if (value is JsonDocument doc)
                    doc.WriteTo(writer);
                else
                    ((JsonElement)value).WriteTo(writer);
                writer.Flush();
                return $"'{EscapeSqlLiteral(Encoding.UTF8.GetString(stream.ToArray()))}'";
            }
            case string s:
                return $"'{EscapeSqlLiteral(s)}'";
            default:  // User POCO
                return $"'{EscapeSqlLiteral(JsonSerializer.Serialize(value))}'";
            }
        }
    }
}
