using System.Diagnostics;
using System.Linq.Expressions;
using FastExpressionCompiler.LightExpression;
using System.Reflection;
using JetBrains.Annotations;
using ConstantExpression = FastExpressionCompiler.LightExpression.ConstantExpression;
using Expression = FastExpressionCompiler.LightExpression.Expression;
using ExpressionVisitor = FastExpressionCompiler.LightExpression.ExpressionVisitor;

namespace StringToExpression;

/// <summary>
/// Provides utility for evaluating expressions.
/// </summary>
[PublicAPI]
public class ExpressionEvaluator : ExpressionVisitor
{
    /// <summary>
    /// Attempts to evaluate the expression locally to produce a result.
    /// </summary>
    /// <typeparam name="T">the result type.</typeparam>
    /// <param name="exp">The expression to evaluate locally.</param>
    /// <param name="result">The output of the expression</param>
    /// <returns><c>true</c> if expression was evaluated; otherwise, <c>false</c>.</returns>
    public static bool TryEvaluate<T>(Expression exp, out T? result)
    {
        if (exp.Type != typeof(T) && typeof(T).GetTypeInfo().IsAssignableFrom(exp.Type.GetTypeInfo()))
        {
            result = default;
            return false;
        }

        //if its a constant we can avoid compiling a lambda and get the value directly
        if (exp is ConstantExpression constExp)
        {
            result = (T)constExp.Value!;
            return true;
        }

        //check to see if we can evaluate
        if (!CanEvaluate(exp))
        {
            result = default;
            return false;
        }

        try
        {
            var lambdaExp = Expression.Lambda<Func<T>>(exp);
            var lambda = lambdaExp.CompileFast();
            result = lambda()!;
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            //we shouldn't get here, but if we do its because something is wrong
            //with the expression, which indicates we cannot evaluate it locally
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Determines whether an expression can be evaluated locally
    /// </summary>
    /// <param name="exp">The expression to evaluate locally.</param>
    /// <returns>
    ///   <c>true</c> if this instance can be evaluated locally; otherwise, <c>false</c>.
    /// </returns>
    public static bool CanEvaluate(Expression exp)
    {
        var evaluator = new ExpressionEvaluator();
        evaluator.Visit(exp);
        return evaluator.CanEvaluateLocally;
    }

    public bool CanEvaluateLocally { get; private set; } = true;

    private ExpressionEvaluator() { }

    public override Expression? Visit(Expression? node)
    {
        if (node is null) throw new ArgumentNullException(nameof(node));
        //if its using a parameter it means we can not evaluate the locally
        CanEvaluateLocally &= node.NodeType != ExpressionType.Parameter;
        return !CanEvaluateLocally ? node : base.Visit(node);
    }
}
