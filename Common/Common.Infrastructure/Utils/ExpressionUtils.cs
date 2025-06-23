using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Common.Infrastructure.Utils;

public static class ExpressionUtils
{
    public static string ToMemberName<TEntity, TResult>(this Expression<Func<TEntity, TResult>> expr)
    {
        if (expr == null)
            throw new ArgumentNullException(nameof(expr));

        if (expr is LambdaExpression lambda)
        {
            var memberExpr = lambda.Body;
            if (lambda.Body is UnaryExpression uexpr)
                memberExpr = uexpr.Operand;

            if (memberExpr is MemberExpression me)
                return me.Member.Name;
        }

        throw new ArgumentException($"Expression {expr} is not member access expression", nameof(expr));
    }

    /// <summary>
    ///     Combines content of first expression and second expression with logical OR
    /// </summary>
    public static Expression<Func<TEntity, bool>> Or<TEntity>(
        this Expression<Func<TEntity, bool>> expr,
        Expression<Func<TEntity, bool>> orExpr
    )
    {
        if (expr is LambdaExpression lambdaExpr)
        {
            var newOrExpr = RewriteParameters(expr, orExpr);

            return Expression.Lambda<Func<TEntity, bool>>(Expression.OrElse(lambdaExpr.Body, newOrExpr), lambdaExpr.Parameters);
        }

        throw new ArgumentException("Expression is not a lambda expression", nameof(expr));
    }


    public static Expression<Func<T, bool>> GreaterThanOrEqual<T>(Expression<Func<T, long>> expr, long value)
        where T : class
    {
        var param = Expression.Parameter(typeof(T), "a");
        var comparison = Expression.GreaterThanOrEqual(RewriteMemberAccess(expr, param), Expression.Constant(value));

        return Expression.Lambda<Func<T, bool>>(comparison, true, param);
    }

    public static Expression<Func<T, bool>> LessThanOrEqual<T>(Expression<Func<T, long>> expr, long value)
        where T : class
    {
        var param = Expression.Parameter(typeof(T), "a");
        var comparison = Expression.LessThanOrEqual(RewriteMemberAccess(expr, param), Expression.Constant(value));

        return Expression.Lambda<Func<T, bool>>(comparison, true, param);
    }

    private static Expression RewriteMemberAccess<T>(Expression<Func<T, long>> expr, ParameterExpression parameter)
        where T : class
    {
        if (expr is LambdaExpression l && l.Body is MemberExpression m)
            return Expression.MakeMemberAccess(parameter, m.Member);
        throw new ArgumentException("Invalid score access expression");
    }

    private static Expression RewriteParameters<T>(Expression<T> from, Expression<T> to)
    {
        var newY = new ParameterVisitor(from.Parameters, to.Parameters).VisitAndConvert(to.Body, "RewriteParameters");

        return newY;
    }
}

internal class ParameterVisitor : ExpressionVisitor
{
    private readonly ReadOnlyCollection<ParameterExpression> _from, _to;

    public ParameterVisitor(ReadOnlyCollection<ParameterExpression> from, ReadOnlyCollection<ParameterExpression> to)
    {
        if (from == null)
            throw new ArgumentNullException(nameof(from));
        if (to == null)
            throw new ArgumentNullException(nameof(to));
        if (from.Count != to.Count)
            throw new InvalidOperationException("Parameter lengths must match");
        _from = from;
        _to = to;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        for (var i = 0; i < _from.Count; i++)
            if (node == _from[i])
                return _to[i];

        return node;
    }
}