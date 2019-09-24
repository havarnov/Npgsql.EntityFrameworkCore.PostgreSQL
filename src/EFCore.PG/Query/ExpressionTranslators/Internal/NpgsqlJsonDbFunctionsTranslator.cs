using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal
{
    public class NpgsqlJsonDbFunctionsTranslator : IMethodCallTranslator
    {
        readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;
        readonly RelationalTypeMapping _boolTypeMapping;
        readonly RelationalTypeMapping _jsonbTypeMapping;

        public NpgsqlJsonDbFunctionsTranslator(NpgsqlSqlExpressionFactory sqlExpressionFactory, IRelationalTypeMappingSource typeMappingSource)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
            _boolTypeMapping = typeMappingSource.FindMapping(typeof(bool));
            _jsonbTypeMapping = typeMappingSource.FindMapping("jsonb");
        }

        public SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments)
        {
            if (method.DeclaringType != typeof(NpgsqlJsonDbFunctionsExtensions))
                return null;

            var args = arguments
                // JSON extensions accept object parameters for JSON, since they must be able to handle POCOs, strings or DOM types.
                // This means they come wrapped in a convert node, which we need to remove.
                // Convert nodes may also come from wrapping JsonTraversalExpressions generated through POCO traversal.
                .Select(RemoveConvert)
                // If a function is invoked over a JSON traversal expression, that expression may come with
                // returnText: true (i.e. operator ->> and not ->). Since the functions below require a json object and
                // not text, we transform it.
                .Select(a => a is JsonTraversalExpression traversal ? traversal.WithReturnsText(false) : a)
                .ToArray();

            if (!args.Any(a => a.TypeMapping is NpgsqlJsonTypeMapping || a is JsonTraversalExpression))
                throw new InvalidOperationException("The EF JSON methods require a JSON parameter and none was found.");
            if (args.Any(a => a.TypeMapping is NpgsqlJsonTypeMapping jsonMapping && !jsonMapping.IsJsonb))
                throw new InvalidOperationException("JSON methods on EF.Functions only support the jsonb type, not json.");

            return method.Name switch
            {
                nameof(NpgsqlJsonDbFunctionsExtensions.JsonContains) => new SqlCustomBinaryExpression(
                    _sqlExpressionFactory.ApplyTypeMapping(args[1], _jsonbTypeMapping),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(args[2]),
                    "@>",
                    typeof(bool),
                    _boolTypeMapping),

                nameof(NpgsqlJsonDbFunctionsExtensions.JsonContained) => new SqlCustomBinaryExpression(
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(args[1]),
                    _sqlExpressionFactory.ApplyTypeMapping(args[2], _jsonbTypeMapping),
                    "<@",
                    typeof(bool),
                    _boolTypeMapping),

                nameof(NpgsqlJsonDbFunctionsExtensions.JsonExists) => new SqlCustomBinaryExpression(
                    _sqlExpressionFactory.ApplyTypeMapping(args[1], _jsonbTypeMapping),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(args[2]),
                    "?",
                    typeof(bool),
                    _boolTypeMapping),

                nameof(NpgsqlJsonDbFunctionsExtensions.JsonExistAny) => new SqlCustomBinaryExpression(
                    _sqlExpressionFactory.ApplyTypeMapping(args[1], _jsonbTypeMapping),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(args[2]),
                    "?|",
                    typeof(bool),
                    _boolTypeMapping),

                nameof(NpgsqlJsonDbFunctionsExtensions.JsonExistAll) => new SqlCustomBinaryExpression(
                    _sqlExpressionFactory.ApplyTypeMapping(args[1], _jsonbTypeMapping),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(args[2]),
                    "?&",
                    typeof(bool),
                    _boolTypeMapping),

                _ => null
            };

            static SqlExpression RemoveConvert(SqlExpression e)
            {
                while (e is SqlUnaryExpression unary &&
                       (unary.OperatorType == ExpressionType.Convert || unary.OperatorType == ExpressionType.ConvertChecked))
                {
                    e = unary.Operand;
                }

                return e;
            }
        }
    }
}
