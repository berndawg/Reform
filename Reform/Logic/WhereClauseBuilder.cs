using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Reform.Interfaces;

namespace Reform.Logic
{
    internal class WhereClauseBuilder<T> : ExpressionVisitor where T : class
    {
        private readonly IMetadataProvider<T> _metadataProvider;
        private readonly IDialect _dialect;
        private StringBuilder _sql;
        private Dictionary<string, object> _parameters;
        private int _parameterIndex;

        public WhereClauseBuilder(IMetadataProvider<T> metadataProvider, IDialect dialect)
        {
            _metadataProvider = metadataProvider;
            _dialect = dialect;
        }

        public (string sql, Dictionary<string, object> parameters) Build(Expression<Func<T, bool>> predicate, int startingIndex = 0)
        {
            _sql = new StringBuilder();
            _parameters = new Dictionary<string, object>();
            _parameterIndex = startingIndex;

            if (predicate == null)
                return (string.Empty, _parameters);

            Visit(predicate.Body);
            return (_sql.ToString(), _parameters);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse)
            {
                _sql.Append("(");
                Visit(node.Left);
                _sql.Append(node.NodeType == ExpressionType.AndAlso ? " AND " : " OR ");
                Visit(node.Right);
                _sql.Append(")");
                return node;
            }

            // Check for null comparisons
            if (IsNullConstant(node.Right))
            {
                VisitMemberForColumn(node.Left);
                _sql.Append(node.NodeType == ExpressionType.Equal ? " IS NULL" : " IS NOT NULL");
                return node;
            }

            if (IsNullConstant(node.Left))
            {
                VisitMemberForColumn(node.Right);
                _sql.Append(node.NodeType == ExpressionType.Equal ? " IS NULL" : " IS NOT NULL");
                return node;
            }

            VisitMemberForColumn(node.Left);
            _sql.Append(GetOperator(node.NodeType));

            object value = GetValue(node.Right);
            string paramName = AddParameter(value);
            _sql.Append($"@{paramName}");

            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
            {
                _sql.Append("NOT (");
                Visit(node.Operand);
                _sql.Append(")");
                return node;
            }

            if (node.NodeType == ExpressionType.Convert)
            {
                Visit(node.Operand);
                return node;
            }

            return base.VisitUnary(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(string))
            {
                switch (node.Method.Name)
                {
                    case "Contains":
                        VisitMemberForColumn(node.Object);
                        var containsValue = EscapeLikeValue(GetValue(node.Arguments[0])?.ToString());
                        var containsParam = AddParameter($"%{containsValue}%");
                        _sql.Append($" LIKE @{containsParam}");
                        return node;

                    case "StartsWith":
                        VisitMemberForColumn(node.Object);
                        var startsValue = EscapeLikeValue(GetValue(node.Arguments[0])?.ToString());
                        var startsParam = AddParameter($"{startsValue}%");
                        _sql.Append($" LIKE @{startsParam}");
                        return node;

                    case "EndsWith":
                        VisitMemberForColumn(node.Object);
                        var endsValue = EscapeLikeValue(GetValue(node.Arguments[0])?.ToString());
                        var endsParam = AddParameter($"%{endsValue}");
                        _sql.Append($" LIKE @{endsParam}");
                        return node;
                }
            }

            throw new NotSupportedException($"Method '{node.Method.Name}' is not supported");
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            // Handle boolean property access (e.g., x => x.IsActive)
            if (node.Type == typeof(bool) && node.Expression is ParameterExpression)
            {
                VisitMemberForColumn(node);
                _sql.Append(" = 1");
                return node;
            }

            return base.VisitMember(node);
        }

        private void VisitMemberForColumn(Expression expression)
        {
            if (expression is UnaryExpression unary)
                expression = unary.Operand;

            if (expression is MemberExpression member)
            {
                var propertyMap = _metadataProvider.GetPropertyMapByPropertyName(member.Member.Name);
                if (propertyMap != null)
                {
                    _sql.Append(_dialect.QuoteIdentifier(propertyMap.ColumnName));
                    return;
                }

                throw new ApplicationException(
                    $"The type '{_metadataProvider.Type}' does not contain property metadata for the property '{member.Member.Name}'");
            }

            throw new NotSupportedException($"Expression type '{expression.NodeType}' is not supported for column access");
        }

        private object GetValue(Expression expression)
        {
            if (expression is ConstantExpression constant)
                return constant.Value;

            if (expression is UnaryExpression { NodeType: ExpressionType.Convert } unary)
                return GetValue(unary.Operand);

            if (expression is MemberExpression member)
            {
                var obj = member.Expression != null ? GetValue(member.Expression) : null;
                return member.Member switch
                {
                    FieldInfo fi => fi.GetValue(obj),
                    PropertyInfo pi => pi.GetValue(obj),
                    _ => CompileAndInvoke(expression)
                };
            }

            return CompileAndInvoke(expression);
        }

        private static object CompileAndInvoke(Expression expression)
        {
            return Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile().Invoke();
        }

        private bool IsNullConstant(Expression expression)
        {
            if (expression is ConstantExpression constant && constant.Value == null)
                return true;

            // Handle Convert(null)
            if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
                return IsNullConstant(unary.Operand);

            return false;
        }

        private string GetOperator(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Equal: return "=";
                case ExpressionType.NotEqual: return "<>";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                default: throw new NotSupportedException($"Operator '{nodeType}' is not supported");
            }
        }

        private string AddParameter(object value)
        {
            _parameterIndex++;
            string name = $"P{_parameterIndex}";
            _parameters.Add(name, value);
            return name;
        }

        private string EscapeLikeValue(string value)
        {
            if (value == null) return null;
            return value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
        }
    }
}
