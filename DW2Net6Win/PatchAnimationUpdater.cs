using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Animations;
using Xenko.Engine;
using Xenko.Updater;

[PublicAPI]
[HarmonyPatch(typeof(AnimationUpdater))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
public static class PatchAnimationUpdater
{
    private static ConditionalWeakTable<AnimationUpdater, Action<Entity, byte[], UpdateObjectData[]>> compiledUpdates = new();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(AnimationUpdater.Update))]
    public static bool Update(AnimationUpdater __instance, Entity entity, AnimationClipResult result,
        ref List<AnimationBlender.Channel> ___currentSourceChannels, ref int ___currentSourceChannelCount)
    {
        var created = false;
        var compiledUpdate = compiledUpdates.GetValue(__instance, au => {
            created = true;
            // nop for now
            return CreateUpdater(result.Channels);
        });

        if (created)
        {
            ___currentSourceChannels = result.Channels;
            ___currentSourceChannelCount = ___currentSourceChannels.Count;
        }
        else if (___currentSourceChannels != result.Channels
                 || ___currentSourceChannels.Count != ___currentSourceChannelCount)
        {
            compiledUpdate = CreateUpdater(result.Channels);
            compiledUpdates.AddOrUpdate(__instance, compiledUpdate);

            ___currentSourceChannels = result.Channels;
            ___currentSourceChannelCount = ___currentSourceChannels.Count;
        }

        compiledUpdate(entity, result.Data, result.Objects);
        return false;
    }


    private static readonly ConstructorInfo ByteSpanCtor =
        typeof(Span<byte>).GetConstructor(new[] { typeof(byte[]), typeof(int), typeof(int) })!;

    private static readonly Regex RxUpdatePathPart = new(
        @"\.((?:[^\.\[])+)(\[[^\]]+\])?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly MethodInfo MemoryMarshalCastSpan = typeof(MemoryMarshal).GetMethods().Single(m
        => m.Name == "Cast" && typeof(Span<>) == m.GetParameters().FirstOrDefault()?.ParameterType.GetGenericTypeDefinition());

    private static Action<Entity, byte[], UpdateObjectData[]> CreateUpdater(List<AnimationBlender.Channel> channels)
    {
        var entityParam = Expression.Parameter(typeof(Entity), "entity");
        var dataParam = Expression.Parameter(typeof(byte[]), "data");
        var objectsParam = Expression.Parameter(typeof(UpdateObjectData[]), "objects");

        var pathExpressionCache = new Dictionary<string, Expression>();
        var pathExpressionRefCounts = new Dictionary<string, int>();

        var vars = new Dictionary<string, ParameterExpression>();
        var varAssignments = new Dictionary<string, Expression>();
        var memberAssignments = new List<Expression>(channels.Count);

        for (var i = 0; i < channels.Count; i++)
        {
            var channel = channels[i];
            if (channel.IsUserCustomProperty)
                continue;

            //[ModelComponent.Key].Skeleton.NodeTransformations[2].Transform.Position
            //typeof(Entity).GetProperty(channel.PropertyName)

            //Entity e;

            var path = channel.PropertyName;
            var pathChars = path.AsSpan();

            Expression expr;

            if (path.StartsWith("[ModelComponent.Key]"))
            {

                if (vars.ContainsKey("[ModelComponent.Key]"))
                    expr = vars["[ModelComponent.Key]"];
                else
                {
                    var entComps = Expression.PropertyOrField(entityParam, nameof(Entity.Components));
                    var getModelComp = entComps.Type.GetMethod("Get", Type.EmptyTypes)!.MakeGenericMethod(typeof(ModelComponent));
                    expr = Expression.Call(entComps, getModelComp);
                    var varExpr = Expression.Variable(typeof(ModelComponent), "var<{[ModelComponent.Key]}>");
                    varAssignments.Add("[ModelComponent.Key]", Expression.Assign(varExpr, expr));
                    vars.Add("[ModelComponent.Key]", varExpr);
                    pathExpressionRefCounts["[ModelComponent.Key]"] = int.MaxValue;
                    expr = varExpr;
                }
                var subPathStart = 20;
                var strOffset = subPathStart;
                var matches = RxUpdatePathPart.Matches(path, strOffset);
                foreach (var match in matches.Cast<Match>())
                {
                    if (match.Index != strOffset)
                    {
                        Console.Error.WriteLine($"warning: unimplemented animation update path, missing component at {strOffset}: {path}");
                        break;
                    }
                    var cacheKeyLength = match.Index + match.Length;
                    var cacheKey = new string(pathChars.Slice(0, cacheKeyLength));
                    if (pathExpressionCache.TryGetValue(cacheKey, out var cachedExpr))
                    {
                        expr = cachedExpr;
                        pathExpressionRefCounts[cacheKey] += 1;
                        //strOffset = subPathStart + cacheKeyLength;
                        strOffset += match.Length;
                        continue;
                    }

                    var groups = match.Groups;
                    var memberName = new string(pathChars.Slice(groups[1].Index, groups[1].Length));
                    var exprType = expr.Type;
                    var member = exprType.GetMember(memberName, MemberTypes.Field | MemberTypes.Property,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).SingleOrDefault();
                    if (member is null)
                    {
                        Console.Error.WriteLine(
                            $"warning: unimplemented animation update path, missing or ambiguous at {groups[1].Index}: {path}");
                        break;
                    }

                    var indexerGroup = groups[2];
                    if (!indexerGroup.Success)
                    {
                        expr = Expression.MakeMemberAccess(expr, member);
                        if (!vars.ContainsKey(cacheKey) && expr.Type.IsClass)
                        {
                            var varExpr = Expression.Variable(expr.Type, $"var<{{{cacheKey}}}>");
                            varAssignments.Add(cacheKey, Expression.Assign(varExpr, expr));
                            vars.Add(cacheKey, varExpr);
                            expr = varExpr;
                        }
                        pathExpressionCache.Add(cacheKey, expr);
                        pathExpressionRefCounts[cacheKey] = 1;
                    }
                    else
                    {
                        if (member is not PropertyInfo memberProp)
                        {
                            Console.Error.WriteLine(
                                $"warning: unimplemented animation update path, non-property indexer at {indexerGroup.Index}: {path}");
                            break;
                        }

                        var indexerSlice = pathChars.Slice(indexerGroup.Index + 1, indexerGroup.Length - 2);

                        if (!int.TryParse(indexerSlice, out var indexValue))
                        {
                            Console.Error.WriteLine(
                                $"warning: unimplemented animation update path, non-integer indexer at {indexerGroup.Index}: {path}");
                            break;
                        }
                        if (memberProp.GetIndexParameters().Length == 1)
                        {
                            expr = Expression.MakeIndex(expr, memberProp, new Expression[] { Expression.Constant(indexValue) });
                            if (!vars.ContainsKey(cacheKey) && expr.Type.IsClass)
                            {
                                var varExpr = Expression.Variable(expr.Type, $"var<{{{cacheKey}}}>");
                                varAssignments.Add(cacheKey, Expression.Assign(varExpr, expr));
                                vars.Add(cacheKey, varExpr);
                                expr = varExpr;
                            }
                            pathExpressionCache.Add(cacheKey, expr);
                            pathExpressionRefCounts[cacheKey] = 1;
                        }
                        else
                        {
                            var subCacheKeyLength = match.Index + (match.Length - indexerGroup.Length);
                            var subCacheKey = new string(pathChars.Slice(0, subCacheKeyLength));
                            if (pathExpressionCache.TryGetValue(subCacheKey, out var subCachedExpr))
                            {
                                expr = subCachedExpr;
                                pathExpressionRefCounts[subCacheKey] += 1;
                            }
                            else
                            {
                                expr = Expression.MakeMemberAccess(expr, member);
                                if (!vars.ContainsKey(subCacheKey) && expr.Type.IsClass)
                                {
                                    var varExpr = Expression.Variable(expr.Type, $"var<{{{subCacheKey}}}>");
                                    varAssignments.Add(subCacheKey, Expression.Assign(varExpr, expr));
                                    vars.Add(subCacheKey, varExpr);
                                    expr = varExpr;
                                }
                                pathExpressionCache.Add(subCacheKey, expr);
                                pathExpressionRefCounts[subCacheKey] = 1;
                            }
                            if (expr.Type.IsSZArray || expr.Type.IsArray)
                            {
                                expr = Expression.ArrayIndex(expr, Expression.Constant(indexValue));
                            }
                            else
                            {
                                var itemProp = expr.Type.GetProperty("Item");
                                if (itemProp is not null)
                                    expr = Expression.MakeIndex(expr, itemProp, new Expression[] { Expression.Constant(indexValue) });
                                else
                                {
                                    Console.Error.WriteLine(
                                        $"warning: unimplemented animation update path, not indexable at {indexerGroup.Index}: {path}");
                                    break;

                                }
                            }
                            if (!vars.ContainsKey(cacheKey) && expr.Type.IsClass)
                            {
                                var varExpr = Expression.Variable(expr.Type, $"var<{{{cacheKey}}}>");
                                varAssignments.Add(cacheKey, Expression.Assign(varExpr, expr));
                                vars.Add(cacheKey, varExpr);
                                expr = varExpr;
                            }
                            pathExpressionCache.Add(cacheKey, expr);
                            pathExpressionRefCounts[cacheKey] = 1;
                        }

                    }
                    strOffset += match.Length;
                }

                var offsetConst = Expression.Constant(channel.Offset);
                var offsetConstPlus1 = Expression.Constant(channel.Offset + sizeof(int) * 1);

                var sizeConst = Expression.Constant(channel.Size);

                var newSpanExpr = Expression.New(
                    ByteSpanCtor,
                    dataParam,
                    offsetConst,
                    sizeConst);

                var newSpanExprOffsetPlus1 = Expression.New(
                    ByteSpanCtor,
                    dataParam,
                    offsetConstPlus1,
                    sizeConst);

                var typedSpanExpr =
                    Expression.Call(MemoryMarshalCastSpan.MakeGenericMethod(typeof(byte), expr.Type), newSpanExprOffsetPlus1);
                Expression typedItemExpr =
                    Expression.Call(MiSpanRead.MakeGenericMethod(expr.Type), typedSpanExpr, Expression.Constant(0));

                Expression readDataCondition = Expression.Call(MiByteArrayReadIntBool, dataParam, offsetConst);

                var updateObjAccess = Expression.ArrayIndex(objectsParam, Expression.Constant(i));
                var updateObjCondition = Expression.NotEqual(Expression.PropertyOrField(updateObjAccess, nameof(UpdateObjectData.Condition)),
                    Expression.Constant(0));

                var updateObjValue = Expression.PropertyOrField(updateObjAccess, nameof(UpdateObjectData.Value));

                switch (channel.BlendType)
                {

                    case AnimationBlender.BlendType.Float1:
                    case AnimationBlender.BlendType.Float2:
                    case AnimationBlender.BlendType.Float3:
                    case AnimationBlender.BlendType.Float4:
                    case AnimationBlender.BlendType.Quaternion:
                    case AnimationBlender.BlendType.Blit: {
                        expr = Expression.IfThen(readDataCondition,
                            Expression.Assign(expr, typedItemExpr)
                        );
                        break;
                    }
                    case AnimationBlender.BlendType.Object: {
                        expr = Expression.IfThen(updateObjCondition,
                            Expression.Assign(expr, Expression.Convert(updateObjValue, expr.Type))
                        );
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(channel.BlendType.ToString());
                }

                memberAssignments.Add(expr);
            }
            else
            {
                Console.Error.WriteLine($"warning: unimplemented animation update path: {path}");
                break;
            }
        }

        var body = Expression.Block(
            vars.OrderByDescending(kv => pathExpressionRefCounts[kv.Key]).Select(kv => kv.Value),
            varAssignments.OrderByDescending(kv => pathExpressionRefCounts[kv.Key]).Select(kv => kv.Value)
                .Concat(memberAssignments));

        var lambda = Expression.Lambda<Action<Entity, byte[], UpdateObjectData[]>>(body, entityParam, dataParam, objectsParam);

        return lambda.Compile();
    }

    public static MethodInfo MiSpanWrite = typeof(PatchAnimationUpdater).GetMethod(nameof(SpanWrite))!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SpanWrite<T>(Span<T> span, int index, T item)
        => span[index] = item;

    public static MethodInfo MiSpanRead = typeof(PatchAnimationUpdater).GetMethod(nameof(SpanRead))!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T SpanRead<T>(Span<T> span, int index)
        => span[index];

    public static MethodInfo MiByteArrayReadIntBool = typeof(PatchAnimationUpdater).GetMethod(nameof(ByteArrayReadIntBool))!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ByteArrayReadIntBool(byte[] bytes, int offset)
        => Unsafe.As<byte, int>(ref bytes[offset]) != 0;
}
