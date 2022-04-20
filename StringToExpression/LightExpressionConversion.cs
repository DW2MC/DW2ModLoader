using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using FastExpressionCompiler.LightExpression;
using JetBrains.Annotations;
using Sys = System.Linq.Expressions;
using Light = FastExpressionCompiler.LightExpression;

namespace StringToExpression;

public static class LightExpressionConversion {

  public static Light.LambdaExpression ToLightExpressions(this Sys.LambdaExpression expr) {
    var body = expr.Body.ToLightExpression();
    var parameters = expr.Parameters.ToLightExpressions();
    var retType = expr.ReturnType;
    return Light.Expression.Lambda(
      body,
      parameters,
      retType);
  }

  public static IEnumerable<Light.Expression?> ToLightExpressions(this IEnumerable<Sys.Expression?> exprs) {
    foreach (var expr in exprs)
      yield return expr.ToLightExpression();
  }

  public static IEnumerable<Light.ParameterExpression> ToLightExpressions(this IEnumerable<Sys.ParameterExpression> exprs) {
    foreach (var expr in exprs)
      yield return expr.ToLightExpression();
  }

  public static Light.BinaryExpression ToLightExpression(this Sys.BinaryExpression expr)
    => Light.Expression.MakeBinary(
      expr.NodeType,
      expr.Left.ToLightExpression(),
      expr.Right.ToLightExpression(),
      expr.IsLiftedToNull,
      expr.Method,
      expr.Conversion?.ToLightExpressions()
    );

  public static Light.BlockExpression ToLightExpression(this Sys.BlockExpression expr)
    => Light.Expression.MakeBlock(
      expr.Type,
      expr.Variables.ToLightExpressions(),
      expr.Expressions.ToLightExpressions()
    );

  public static Light.ConditionalExpression ToLightExpression(this Sys.ConditionalExpression expr)
    => Light.Expression.Condition(
      expr.Test.ToLightExpression(),
      expr.IfTrue.ToLightExpression(),
      expr.IfFalse.ToLightExpression()
    );

  public static Light.ConstantExpression ToLightExpression(this Sys.ConstantExpression expr)
    => Light.Expression.Constant(
      expr.Value,
      expr.Type
    );

  public static Light.DebugInfoExpression ToLightExpression(this Sys.DebugInfoExpression expr)
    => (Light.DebugInfoExpression)
      Light.Expression.DebugInfo(
        expr.Document.ToLightExpression(),
        expr.StartLine,
        expr.StartColumn,
        expr.EndLine,
        expr.EndColumn
      );

  public static Light.SymbolDocumentInfo ToLightExpression(this Sys.SymbolDocumentInfo doc)
    => Light.Expression.SymbolDocument(doc.FileName);

  public static Light.DefaultExpression ToLightExpression(this Sys.DefaultExpression expr)
    => Light.Expression.Default(expr.Type);

  public static Light.DynamicExpression ToLightExpression(this Sys.DynamicExpression expr)
    => new(
      expr.DelegateType,
      expr.Binder,
      expr.Arguments.ToLightExpressions().ToList()
    );

  public static readonly ConditionalWeakTable<Sys.LabelTarget, Light.LabelTarget> LabelResolver = new();

  public static Light.GotoExpression ToLightExpression(this Sys.GotoExpression expr)
    => Light.Expression.MakeGoto(
      expr.Kind,
      expr.Target.ToLightExpression(),
      expr.Value.ToLightExpression(),
      expr.Type
    );

  public static Light.LabelTarget ToLightExpression(this Sys.LabelTarget expr)
    => LabelResolver.GetValue(expr, LabelTargetFactory);

  private static Light.LabelTarget LabelTargetFactory(Sys.LabelTarget expr)
    => expr.Name is not null
      ? new TypedNamedLabelTarget(expr.Type, expr.Name)
      : new TypedLabelTarget(expr.Type);
  /*
  private static Light.LabelTarget LabelTargetFactory(Sys.LabelTarget expr)
    => expr.Name is not null
      ? expr.Type is not null
        ? new TypedNamedLabelTarget(expr.Type, expr.Name)
        : new NamedLabelTarget(expr.Name)
      : expr.Type is not null
        ? new TypedLabelTarget(expr.Type)
        : new Light.LabelTarget();
   */

  public static Light.IndexExpression ToLightExpression(this Sys.IndexExpression expr)
    => Light.Expression.MakeIndex(
      expr.Object.ToLightExpression(),
      expr.Indexer,
      expr.Arguments.ToLightExpressions()
    );

  public static Light.InvocationExpression ToLightExpression(this Sys.InvocationExpression expr)
    => Light.Expression.Invoke(
      expr.Expression.ToLightExpression(),
      expr.Arguments.ToLightExpressions()
    );

  public static Light.LabelExpression ToLightExpression(this Sys.LabelExpression expr)
    => Light.Expression.Label(
      expr.Target.ToLightExpression(),
      expr.DefaultValue.ToLightExpression()
    );

  public static Light.ListInitExpression ToLightExpression(this Sys.ListInitExpression expr)
    => Light.Expression.ListInit(
      expr.NewExpression.ToLightExpression(),
      expr.Initializers.ToLightExpressions()
    );

  public static Light.ElementInit ToLightExpression(this Sys.ElementInit init)
    => init.Arguments.Count == 1
      ? Light.Expression.ElementInit(init.AddMethod, init.Arguments[0].ToLightExpression())
      : Light.Expression.ElementInit(init.AddMethod, init.Arguments.ToLightExpressions());

  public static IEnumerable<Light.ElementInit> ToLightExpressions(this IEnumerable<Sys.ElementInit> inits) {
    foreach (var init in inits)
      yield return init.ToLightExpression();
  }

  public static Light.NewExpression ToLightExpression(this Sys.NewExpression expr) {
    var args = expr.Arguments;
    return args.Count switch {
      0 => Light.Expression.New(expr.Constructor),
      1 => Light.Expression.New(expr.Constructor,
        args[0].ToLightExpression()),
      2 => Light.Expression.New(expr.Constructor,
        args[0].ToLightExpression(),
        args[1].ToLightExpression()),
      3 => Light.Expression.New(expr.Constructor,
        args[0].ToLightExpression(),
        args[1].ToLightExpression(),
        args[2].ToLightExpression()),
      4 => Light.Expression.New(expr.Constructor,
        args[0].ToLightExpression(),
        args[1].ToLightExpression(),
        args[2].ToLightExpression(),
        args[3].ToLightExpression()),
      5 => Light.Expression.New(expr.Constructor,
        args[0].ToLightExpression(),
        args[1].ToLightExpression(),
        args[2].ToLightExpression(),
        args[3].ToLightExpression(),
        args[4].ToLightExpression()),
      6 => Light.Expression.New(expr.Constructor,
        args[0].ToLightExpression(),
        args[1].ToLightExpression(),
        args[2].ToLightExpression(),
        args[3].ToLightExpression(),
        args[4].ToLightExpression(),
        args[5].ToLightExpression()),
      7 => Light.Expression.New(expr.Constructor,
        args[0].ToLightExpression(),
        args[1].ToLightExpression(),
        args[2].ToLightExpression(),
        args[3].ToLightExpression(),
        args[4].ToLightExpression(),
        args[5].ToLightExpression(),
        args[6].ToLightExpression()),
      _ => Light.Expression.New(expr.Constructor, args.ToLightExpressions())
    };
  }

  public static Light.LoopExpression ToLightExpression(this Sys.LoopExpression expr)
    => Light.Expression.Loop(
      expr.Body.ToLightExpression(),
      expr.BreakLabel?.ToLightExpression(),
      expr.ContinueLabel?.ToLightExpression()
    );

  public static Light.MemberExpression ToLightExpression(this Sys.MemberExpression expr)
    => Light.Expression.MakeMemberAccess(
      expr.Expression.ToLightExpression(),
      expr.Member
    );

  public static Light.MemberInitExpression ToLightExpression(this Sys.MemberInitExpression expr) {
    var binds = expr.Bindings;
    return binds.Count switch {
      0 => Light.Expression.MemberInit(expr.NewExpression.ToLightExpression()),
      1 => Light.Expression.MemberInit(expr.NewExpression.ToLightExpression(),
        binds[0].ToLightExpression()),
      2 => Light.Expression.MemberInit(expr.NewExpression.ToLightExpression(),
        binds[0].ToLightExpression(),
        binds[1].ToLightExpression()),
      3 => Light.Expression.MemberInit(expr.NewExpression.ToLightExpression(),
        binds[0].ToLightExpression(),
        binds[1].ToLightExpression(),
        binds[2].ToLightExpression()),
      4 => Light.Expression.MemberInit(expr.NewExpression.ToLightExpression(),
        binds[0].ToLightExpression(),
        binds[1].ToLightExpression(),
        binds[2].ToLightExpression(),
        binds[3].ToLightExpression()),
      5 => Light.Expression.MemberInit(expr.NewExpression.ToLightExpression(),
        binds[0].ToLightExpression(),
        binds[1].ToLightExpression(),
        binds[2].ToLightExpression(),
        binds[3].ToLightExpression(),
        binds[4].ToLightExpression()),
      6 => Light.Expression.MemberInit(expr.NewExpression.ToLightExpression(),
        binds[0].ToLightExpression(),
        binds[1].ToLightExpression(),
        binds[2].ToLightExpression(),
        binds[3].ToLightExpression(),
        binds[4].ToLightExpression(),
        binds[6].ToLightExpression()),
      _ => Light.Expression.MemberInit(expr.NewExpression.ToLightExpression(),
        binds.ToLightExpressions())
    };
  }

  public static Light.MemberBinding ToLightExpression(this Sys.MemberBinding expr) {
    switch (expr) {
      case Sys.MemberMemberBinding m:
        return Light.Expression.MemberBind(m.Member, m.Bindings.ToLightExpressions());
      case Sys.MemberListBinding m:
        return Light.Expression.ListBind(m.Member, m.Initializers.ToLightExpressions());
      case Sys.MemberAssignment m:
        return Light.Expression.Bind(m.Member, m.Expression.ToLightExpression());
      default: throw new NotImplementedException(expr.GetType().AssemblyQualifiedName);
    }
  }

  public static IEnumerable<Light.MemberBinding> ToLightExpressions(this IEnumerable<Sys.MemberBinding> binds) {
    foreach (var bind in binds)
      yield return bind.ToLightExpression();
  }

  public static Light.MethodCallExpression ToLightExpression(this Sys.MethodCallExpression expr) {
    var args = expr.Arguments;
    if (expr.Method.IsStatic) {
      return args.Count switch {
        0 => Light.Expression.Call(expr.Method),
        1 => Light.Expression.Call(expr.Method,
          args[0].ToLightExpression()),
        2 => Light.Expression.Call(expr.Method,
          args[0].ToLightExpression(),
          args[1].ToLightExpression()),
        3 => Light.Expression.Call(expr.Method,
          args[0].ToLightExpression(),
          args[1].ToLightExpression(),
          args[2].ToLightExpression()),
        4 => Light.Expression.Call(expr.Method,
          args[0].ToLightExpression(),
          args[1].ToLightExpression(),
          args[2].ToLightExpression(),
          args[3].ToLightExpression()),
        5 => Light.Expression.Call(expr.Method,
          args[0].ToLightExpression(),
          args[1].ToLightExpression(),
          args[2].ToLightExpression(),
          args[3].ToLightExpression(),
          args[4].ToLightExpression()),
        6 => Light.Expression.Call(expr.Method,
          args[0].ToLightExpression(),
          args[1].ToLightExpression(),
          args[2].ToLightExpression(),
          args[3].ToLightExpression(),
          args[4].ToLightExpression(),
          args[5].ToLightExpression()),
        _ => Light.Expression.Call(expr.Method,
          args.ToLightExpressions())
      };
    }
    else {
      return args.Count switch {
        0 => Light.Expression.Call(
          expr.Object.ToLightExpression(),
          expr.Method),
        1 => Light.Expression.Call(
          expr.Object.ToLightExpression(),
          expr.Method,
          args[0].ToLightExpression()),
        2 => Light.Expression.Call(
          expr.Object.ToLightExpression(),
          expr.Method,
          args[0].ToLightExpression(),
          args[1].ToLightExpression()),
        3 => Light.Expression.Call(
          expr.Object.ToLightExpression(),
          expr.Method,
          args[0].ToLightExpression(),
          args[1].ToLightExpression(),
          args[2].ToLightExpression()),
        4 => Light.Expression.Call(
          expr.Object.ToLightExpression(),
          expr.Method,
          args[0].ToLightExpression(),
          args[1].ToLightExpression(),
          args[2].ToLightExpression(),
          args[3].ToLightExpression()),
        5 => Light.Expression.Call(
          expr.Object.ToLightExpression(),
          expr.Method,
          args[0].ToLightExpression(),
          args[1].ToLightExpression(),
          args[2].ToLightExpression(),
          args[3].ToLightExpression(),
          args[4].ToLightExpression()),
        6 => Light.Expression.Call(
          expr.Object.ToLightExpression(),
          expr.Method,
          args[0].ToLightExpression(),
          args[1].ToLightExpression(),
          args[2].ToLightExpression(),
          args[3].ToLightExpression(),
          args[4].ToLightExpression(),
          args[5].ToLightExpression()),
        _ => Light.Expression.Call(
          expr.Object.ToLightExpression(),
          expr.Method,
          args.ToLightExpressions())
      };
    }
  }

  public static Light.NewArrayExpression ToLightExpression(this Sys.NewArrayExpression expr) {
    var vals = expr.Expressions;
    return vals.Count switch {
      0 => Light.Expression.NewArrayInit(expr.Type),
      1 => Light.Expression.NewArrayInit(expr.Type,
        vals[0].ToLightExpression()),
      2 => Light.Expression.NewArrayInit(expr.Type,
        vals[0].ToLightExpression(),
        vals[1].ToLightExpression()),
      3 => Light.Expression.NewArrayInit(expr.Type,
        vals[0].ToLightExpression(),
        vals[1].ToLightExpression(),
        vals[2].ToLightExpression()),
      4 => Light.Expression.NewArrayInit(expr.Type,
        vals[0].ToLightExpression(),
        vals[1].ToLightExpression(),
        vals[2].ToLightExpression(),
        vals[3].ToLightExpression()),
      5 => Light.Expression.NewArrayInit(expr.Type,
        vals[0].ToLightExpression(),
        vals[1].ToLightExpression(),
        vals[2].ToLightExpression(),
        vals[3].ToLightExpression(),
        vals[4].ToLightExpression()),
      6 => Light.Expression.NewArrayInit(expr.Type,
        vals[0].ToLightExpression(),
        vals[1].ToLightExpression(),
        vals[2].ToLightExpression(),
        vals[3].ToLightExpression(),
        vals[4].ToLightExpression(),
        vals[5].ToLightExpression()),
      _ => Light.Expression.NewArrayInit(expr.Type,
        vals.ToLightExpressions())
    };
  }

  public static readonly ConditionalWeakTable<Sys.ParameterExpression, Light.ParameterExpression> ParameterResolver = new();

  public static Light.ParameterExpression ToLightExpression(this Sys.ParameterExpression expr)
    => ParameterResolver.GetValue(expr, ParameterFactory);

  private static Light.ParameterExpression ParameterFactory(Sys.ParameterExpression expr)
    => expr.IsByRef
      ? throw new NotImplementedException("expr.IsByRef")
      : expr.Type.IsByRef
        // both are parameter expressions
        ? Light.Expression.Parameter(expr.Type, expr.Name)
        : Light.Expression.Variable(expr.Type, expr.Name);

  public static Light.RuntimeVariablesExpression ToLightExpression(this Sys.RuntimeVariablesExpression expr)
    => throw new NotImplementedException();

  public static Light.SwitchExpression ToLightExpression(this Sys.SwitchExpression expr) {
    return Light.Expression.Switch(
      expr.Type,
      expr.SwitchValue.ToLightExpression(),
      expr.DefaultBody.ToLightExpression(),
      expr.Comparison,
      expr.Cases.ToLightExpressions());
  }

  public static IEnumerable<Light.SwitchCase> ToLightExpressions(this IEnumerable<Sys.SwitchCase> cases) {
    foreach (var expr in cases)
      yield return expr.ToLightExpression();
  }

  public static Light.SwitchCase ToLightExpression(this Sys.SwitchCase expr)
    => Light.Expression.SwitchCase(expr.Body.ToLightExpression(), expr.TestValues.ToLightExpressions());

  public static Light.TryExpression ToLightExpression(this Sys.TryExpression expr)
    => Light.Expression.TryCatch(expr.Body.ToLightExpression(), expr.Handlers.ToLightExpressions().ToArray());

  public static IEnumerable<Light.CatchBlock> ToLightExpressions(this IEnumerable<Sys.CatchBlock> blocks) {
    foreach (var block in blocks)
      yield return block.ToLightExpression();
  }

  public static Light.CatchBlock ToLightExpression(this Sys.CatchBlock expr)
    => Light.Expression.MakeCatchBlock(expr.Test,
      expr.Variable?.ToLightExpression(),
      expr.Body.ToLightExpression(),
      expr.Filter.ToLightExpression());

  public static Light.TypeBinaryExpression ToLightExpression(this Sys.TypeBinaryExpression expr) {
    var exprType = expr.NodeType;
    return exprType switch {
      ExpressionType.TypeIs => Light.Expression.TypeIs(expr.Expression.ToLightExpression(), expr.TypeOperand),
      ExpressionType.TypeEqual => Light.Expression.TypeEqual(expr.Expression.ToLightExpression(), expr.TypeOperand),
      _ => throw new NotImplementedException(exprType.ToString())
    };
  }

  public static Light.UnaryExpression ToLightExpression(this Sys.UnaryExpression expr)
    => Light.Expression.MakeUnary(expr.NodeType, expr.Operand.ToLightExpression(), expr.Type);

  public static Light.LambdaExpression ToLightExpression(this Sys.LambdaExpression expr)
    => Light.Expression.Lambda(expr.Body.ToLightExpression(), expr.Parameters.ToLightExpressions(), expr.Type);

  [ContractAnnotation("null => null; notnull => notnull")]
  public static Light.Expression? ToLightExpression(this Sys.Expression? expr) {
    if (expr is null) return null;

    switch (expr) {
      case Sys.BinaryExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.BlockExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.ConditionalExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.ConstantExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.DebugInfoExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.DefaultExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.DynamicExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.GotoExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.IndexExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.InvocationExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.LabelExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.LambdaExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.ListInitExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.LoopExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.MemberExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.MemberInitExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.MethodCallExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.NewArrayExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.NewExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.ParameterExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.RuntimeVariablesExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.SwitchExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.TryExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.TypeBinaryExpression typedExpr: return typedExpr.ToLightExpression();
      case Sys.UnaryExpression typedExpr: return typedExpr.ToLightExpression();
      default: throw new NotImplementedException(expr.GetType().AssemblyQualifiedName);
    }
  }

}