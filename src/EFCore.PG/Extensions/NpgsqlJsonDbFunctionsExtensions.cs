using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Extensions
{
    /// <summary>
    /// Provides methods for supporting translation to PostgreSQL JSON operators and functions.
    /// </summary>
    public static class NpgsqlJsonDbFunctionsExtensions
    {
        /// <summary>
        /// Checks if <paramref name="json"/> contains <paramref name="contained"/> as top-level entries.
        /// </summary>
        /// <param name="_">DbFunctions instance</param>
        /// <param name="json">
        /// A JSON column or value. Can be a <see cref="JsonDocument"/>, a string property mapped to JSON,
        /// or a user POCO mapped to JSON.
        /// </param>
        /// <param name="contained">A JSON text fragment.</param>
        public static bool JsonContains(this DbFunctions _, object json, string contained)
            => throw ClientEvaluationNotSupportedException();

        /// <summary>
        /// Checks if <paramref name="json"/> contains <paramref name="contained"/> as top-level entries.
        /// </summary>
        /// <param name="_">DbFunctions instance</param>
        /// <param name="json">
        /// A JSON column or value. Can be a <see cref="JsonDocument"/>, a string property mapped to JSON,
        /// or a user POCO mapped to JSON.
        /// </param>
        /// <param name="contained">
        /// A JSON fragment. Can be a <see cref="JsonDocument"/>, a string property mapped to JSON,
        /// or a user POCO mapped to JSON.
        /// </param>
        public static bool JsonContains(this DbFunctions _, object json, object contained)
            => throw ClientEvaluationNotSupportedException();

        /// <summary>
        /// Checks if <paramref name="contained"/> is contained in <paramref name="json"/> as top-level entries.
        /// </summary>
        /// <param name="_">DbFunctions instance</param>
        /// <param name="contained">A JSON text fragment.</param>
        /// <param name="json">
        /// A JSON column or value. Can be a <see cref="JsonDocument"/>, a string property mapped to JSON,
        /// or a user POCO mapped to JSON.
        /// </param>
        public static bool JsonContained(this DbFunctions _, string contained, object json)
            => throw ClientEvaluationNotSupportedException();

        /// <summary>
        /// Checks if <paramref name="contained"/> is contained in <paramref name="json"/> as top-level entries.
        /// </summary>
        /// <param name="_">DbFunctions instance</param>
        /// <param name="contained">
        /// A JSON fragment. Can be a <see cref="JsonDocument"/>, a string property mapped to JSON,
        /// or a user POCO mapped to JSON.
        /// </param>
        /// <param name="json">
        /// A JSON column or value. Can be a <see cref="JsonDocument"/>, a string property mapped to JSON,
        /// or a user POCO mapped to JSON.
        /// </param>
        public static bool JsonContained(this DbFunctions _, object contained, object json)
            => throw ClientEvaluationNotSupportedException();

        /// <summary>
        /// Checks if <paramref name="key"/> exists as a top-level key within <paramref name="json"/>.
        /// </summary>
        /// <param name="_">DbFunctions instance</param>
        /// <param name="json">
        /// A JSON column or value. Can be a <see cref="JsonDocument"/>, a string property mapped to JSON,
        /// or a user POCO mapped to JSON.
        /// </param>
        /// <param name="key">A key to be checked inside <paramref name="json"/>.</param>
        public static bool JsonExists(this DbFunctions _, object json, string key)
            => throw ClientEvaluationNotSupportedException();

        /// <summary>
        /// Checks if any of the given <paramref name="keys"/> exist as top-level keys within <paramref name="json"/>.
        /// </summary>
        /// <param name="_">DbFunctions instance</param>
        /// <param name="json">
        /// A JSON column or value. Can be a <see cref="JsonDocument"/>, a string property mapped to JSON,
        /// or a user POCO mapped to JSON.
        /// </param>
        /// <param name="keys">A set of keys to be checked inside <paramref name="json"/>.</param>
        public static bool JsonExistAny(this DbFunctions _, object json, params string[] keys)
            => throw ClientEvaluationNotSupportedException();

        /// <summary>
        /// Checks if all of the given <paramref name="keys"/> exist as top-level keys within <paramref name="json"/>.
        /// </summary>
        /// <param name="_">DbFunctions instance</param>
        /// <param name="json">
        /// A JSON column or value. Can be a <see cref="JsonDocument"/>, a string property mapped to JSON,
        /// or a user POCO mapped to JSON.
        /// </param>
        /// <param name="keys">A set of keys to be checked inside <paramref name="json"/>.</param>
        public static bool JsonExistAll(this DbFunctions _, object json, params string[] keys)
            => throw ClientEvaluationNotSupportedException();

        static NotSupportedException ClientEvaluationNotSupportedException([CallerMemberName] string method = default)
            => new NotSupportedException($"{method} is only intended for use via SQL translation as part of an EF Core LINQ query.");
    }
}
