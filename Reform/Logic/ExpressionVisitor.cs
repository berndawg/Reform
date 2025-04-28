using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    public class SqlExpressionVisitor : ExpressionVisitor
    {
        private readonly StringBuilder _sql;
        private readonly Dictionary<string, object> _parameters;
        private readonly IColumnNameFormatter _columnNameFormatter;
        private int _parameterCount;
        private bool _isAggregateContext;

        public SqlExpressionVisitor(IColumnNameFormatter columnNameFormatter)
        {
            _sql = new StringBuilder();
            _parameters = new Dictionary<string, object>();
            _columnNameFormatter = columnNameFormatter;
            _parameterCount = 0;
            _isAggregateContext = false;
        }

        public (string Sql, Dictionary<string, object> Parameters) GetResult()
        {
            return (_sql.ToString(), _parameters);
        }

        public void VisitAggregate(string function, Expression property, string alias)
        {
            _sql.Append(function);
            _sql.Append("(");
            
            if (property != null)
            {
                _isAggregateContext = true;
                Visit(property);
                _isAggregateContext = false;
            }
            else
            {
                _sql.Append("*");
            }
            
            _sql.Append(")");

            if (!string.IsNullOrEmpty(alias))
            {
                _sql.Append(" AS ");
                _sql.Append(_columnNameFormatter.Format(alias));
            }
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (IsNullConstant(node.Right))
            {
                Visit(node.Left);
                switch (node.NodeType)
                {
                    case ExpressionType.Equal:
                        _sql.Append(" IS NULL");
                        return node;
                    case ExpressionType.NotEqual:
                        _sql.Append(" IS NOT NULL");
                        return node;
                }
            }

            _sql.Append("(");
            Visit(node.Left);

            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    _sql.Append(" = ");
                    break;
                case ExpressionType.NotEqual:
                    _sql.Append(" <> ");
                    break;
                case ExpressionType.GreaterThan:
                    _sql.Append(" > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    _sql.Append(" >= ");
                    break;
                case ExpressionType.LessThan:
                    _sql.Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    _sql.Append(" <= ");
                    break;
                case ExpressionType.AndAlso:
                    _sql.Append(" AND ");
                    break;
                case ExpressionType.OrElse:
                    _sql.Append(" OR ");
                    break;
                case ExpressionType.Add:
                    _sql.Append(" + ");
                    break;
                case ExpressionType.Subtract:
                    _sql.Append(" - ");
                    break;
                case ExpressionType.Multiply:
                    _sql.Append(" * ");
                    break;
                case ExpressionType.Divide:
                    _sql.Append(" / ");
                    break;
                case ExpressionType.Modulo:
                    _sql.Append(" % ");
                    break;
                default:
                    throw new NotSupportedException($"The binary operator '{node.NodeType}' is not supported");
            }

            Visit(node.Right);
            _sql.Append(")");

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression != null && node.Expression.NodeType == ExpressionType.Parameter)
            {
                _sql.Append(_columnNameFormatter.Format(node.Member.Name));
                return node;
            }

            if (_isAggregateContext)
            {
                _sql.Append(_columnNameFormatter.Format(node.Member.Name));
                return node;
            }

            var value = Expression.Lambda(node).Compile().DynamicInvoke();
            string paramName = $"@p{++_parameterCount}";
            _parameters.Add(paramName, value ?? DBNull.Value);
            _sql.Append(paramName);

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value == null)
            {
                _sql.Append("NULL");
                return node;
            }

            string paramName = $"@p{++_parameterCount}";
            _parameters.Add(paramName, node.Value);
            _sql.Append(paramName);
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(string))
            {
                switch (node.Method.Name)
                {
                    case "Contains":
                        Visit(node.Object);
                        _sql.Append(" LIKE CONCAT('%', ");
                        Visit(node.Arguments[0]);
                        _sql.Append(", '%')");
                        return node;
                    case "StartsWith":
                        Visit(node.Object);
                        _sql.Append(" LIKE CONCAT(");
                        Visit(node.Arguments[0]);
                        _sql.Append(", '%')");
                        return node;
                    case "EndsWith":
                        Visit(node.Object);
                        _sql.Append(" LIKE CONCAT('%', ");
                        Visit(node.Arguments[0]);
                        _sql.Append(")");
                        return node;
                    case "ToUpper":
                        _sql.Append("UPPER(");
                        Visit(node.Object);
                        _sql.Append(")");
                        return node;
                    case "ToLower":
                        _sql.Append("LOWER(");
                        Visit(node.Object);
                        _sql.Append(")");
                        return node;
                    case "Trim":
                        _sql.Append("TRIM(");
                        Visit(node.Object);
                        _sql.Append(")");
                        return node;
                    case "Substring":
                        _sql.Append("SUBSTRING(");
                        Visit(node.Object);
                        _sql.Append(", ");
                        Visit(node.Arguments[0]);
                        if (node.Arguments.Count > 1)
                        {
                            _sql.Append(", ");
                            Visit(node.Arguments[1]);
                        }
                        _sql.Append(")");
                        return node;
                }
            }
            else if (node.Method.DeclaringType == typeof(Math))
            {
                switch (node.Method.Name)
                {
                    case "Abs":
                        _sql.Append("ABS(");
                        Visit(node.Arguments[0]);
                        _sql.Append(")");
                        return node;
                    case "Round":
                        _sql.Append("ROUND(");
                        Visit(node.Arguments[0]);
                        if (node.Arguments.Count > 1)
                        {
                            _sql.Append(", ");
                            Visit(node.Arguments[1]);
                        }
                        _sql.Append(")");
                        return node;
                    case "Floor":
                        _sql.Append("FLOOR(");
                        Visit(node.Arguments[0]);
                        _sql.Append(")");
                        return node;
                    case "Ceiling":
                        _sql.Append("CEILING(");
                        Visit(node.Arguments[0]);
                        _sql.Append(")");
                        return node;
                }
            }

            throw new NotSupportedException($"The method '{node.Method.Name}' is not supported");
        }

        private bool IsNullConstant(Expression expression)
        {
            return expression.NodeType == ExpressionType.Constant && 
                   ((ConstantExpression)expression).Value == null;
        }
    }
} 