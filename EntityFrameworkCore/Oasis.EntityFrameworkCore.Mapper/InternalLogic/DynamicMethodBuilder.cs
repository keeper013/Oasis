﻿namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Reflection.Emit;

internal enum KeyType
{
    /// <summary>
    /// Id
    /// </summary>
    Id,

    /// <summary>
    /// Concurrenty Token
    /// </summary>
    ConcurrencyToken,
}

internal interface IDynamicMethodBuilder
{
    MethodMetaData? BuildUpKeyPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo? sourceIdentityProperty,
        PropertyInfo? targetIdentityProperty,
        PropertyInfo? sourceConcurrencyTokenProperty,
        PropertyInfo? targetConcurrencyTokenProperty);

    MethodMetaData? BuildUpScalarPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        IList<(PropertyInfo, PropertyInfo)> matchedProperties);

    MethodMetaData? BuildUpEntityPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        IList<(PropertyInfo, Type, PropertyInfo, Type)> matchedProperties);

    public MethodMetaData? BuildUpEntityListPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        IList<(PropertyInfo, Type, PropertyInfo, Type)> matchedProperties);

    MethodMetaData BuildUpKeyEqualComparerMethod(
        KeyType keyType,
        Type sourceType,
        Type targetType,
        PropertyInfo sourceKeyProperty,
        PropertyInfo targetKeyProperty);

    // concurrency token doesn't need get method, so the only get method is for id
    MethodMetaData BuildUpGetIdMethod(KeyType keyType, Type type, PropertyInfo identityProperty);

    MethodMetaData BuildUpKeyIsEmptyMethod(KeyType keyType, Type type, PropertyInfo identityProperty);

    Type Build();
}

internal sealed class DynamicMethodBuilder : IDynamicMethodBuilder
{
    private const char MapScalarPropertiesMethod = 's';
    private const char MapKeyPropertiesMethod = 'k';
    private const char MapEntityPropertiesMethod = 'e';
    private const char MapListPropertiesMethod = 'l';
    private const char CompareIdMethod = 'i';
    private const char CompareConcurrencyTokenMethod = 'o';
    private const char GetId = 'd';
    private const char IdEmpty = 'b';
    private const char ConcurrencyTokenEmpty = 'n';

    private static readonly MethodInfo ObjectEqual = typeof(object).GetMethod(nameof(object.Equals), new[] { typeof(object) })!;
    private static readonly ConstructorInfo MissingSetting = typeof(SetterMissingException).GetConstructor(Utilities.PublicInstance, new[] { typeof(string) })!;

    private readonly GenericMapperMethodCache _scalarPropertyConverterCache = new (typeof(IScalarTypeConverter).GetMethods().First(m => string.Equals(m.Name, nameof(IScalarTypeConverter.Convert)) && m.IsGenericMethod));
    private readonly GenericMapperMethodCache _entityPropertyMapperCache = new (typeof(IEntityPropertyMapper<int>).GetMethod(nameof(IEntityPropertyMapper<int>.MapEntityProperty), Utilities.PublicInstance)!);
    private readonly GenericMapperMethodCache _entityListPropertyMapperCache = new (typeof(IListPropertyMapper<int>).GetMethod(nameof(IListPropertyMapper<int>.MapListProperty), Utilities.PublicInstance)!);
    private readonly GenericMapperMethodCache _listTypeConstructorCache = new (typeof(IListPropertyMapper<int>).GetMethod(nameof(IListPropertyMapper<int>.ConstructListType), Utilities.PublicInstance)!);
    private readonly IScalarTypeMethodCache _isDefaultValueCache = new ScalarTypeMethodCache(typeof(ScalarTypeIsDefaultValueMethods), nameof(ScalarTypeIsDefaultValueMethods.IsDefaultValue), new[] { typeof(object) });
    private readonly IScalarTypeMethodCache _areEqualCache = new ScalarTypeMethodCache(typeof(ScalarTypeEqualMethods), nameof(ScalarTypeEqualMethods.AreEqual), new[] { typeof(object), typeof(object) });
    private readonly TypeBuilder _typeBuilder;

    public DynamicMethodBuilder(TypeBuilder typeBuilder)
    {
        _typeBuilder = typeBuilder;
    }

    public Type Build()
    {
        return _typeBuilder.CreateType()!;
    }

    public MethodMetaData? BuildUpKeyPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo? sourceIdentityProperty,
        PropertyInfo? targetIdentityProperty,
        PropertyInfo? sourceConcurrencyTokenProperty,
        PropertyInfo? targetConcurrencyTokenProperty)
    {
        bool generateForIdentity = sourceIdentityProperty != default && targetIdentityProperty != default;
        bool generateForConcurrencyToken = sourceConcurrencyTokenProperty != default && targetConcurrencyTokenProperty != default;
        if (!generateForIdentity && !generateForConcurrencyToken)
        {
            return null;
        }

        var methodName = BuildMapperMethodName(MapKeyPropertiesMethod, sourceType, targetType);
        var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IScalarTypeConverter) }, typeof(void));
        var generator = method.GetILGenerator();

        if (generateForIdentity)
        {
            GenerateScalarPropertyValueAssignmentIL(generator, sourceIdentityProperty!, targetIdentityProperty!);
        }

        if (generateForConcurrencyToken)
        {
            GenerateScalarPropertyValueAssignmentIL(generator, sourceConcurrencyTokenProperty!, targetConcurrencyTokenProperty!);
        }

        generator.Emit(OpCodes.Ret);
        return new MethodMetaData(typeof(Utilities.MapScalarProperties<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    public MethodMetaData? BuildUpScalarPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        IList<(PropertyInfo, PropertyInfo)> matchedProperties)
    {
        var methodName = BuildMapperMethodName(MapScalarPropertiesMethod, sourceType, targetType);
        var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IScalarTypeConverter) }, typeof(void));
        var generator = method.GetILGenerator();
        foreach (var match in matchedProperties)
        {
            GenerateScalarPropertyValueAssignmentIL(generator, match.Item1, match.Item2);
        }

        generator.Emit(OpCodes.Ret);
        return new MethodMetaData(typeof(Utilities.MapScalarProperties<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    public MethodMetaData? BuildUpEntityPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        IList<(PropertyInfo, Type, PropertyInfo, Type)> matchedProperties)
    {
        var methodName = BuildMapperMethodName(MapEntityPropertiesMethod, sourceType, targetType);
        var method = BuildMethod(
            methodName,
            new[] { typeof(IEntityPropertyMapper<int>), sourceType, targetType, typeof(INewTargetTracker<int>), typeof(EntityPropertyMappingData?) },
            typeof(void));
        var generator = method.GetILGenerator();
        foreach (var matched in matchedProperties)
        {
            // now it's made sure that mapper between entities exists, emit the entity property mapping code
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, matched.Item1.GetMethod!);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Callvirt, matched.Item3.GetMethod!);
            generator.Emit(OpCodes.Ldarg_3);
            generator.Emit(OpCodes.Ldarg, 4);
            generator.Emit(OpCodes.Callvirt, _entityPropertyMapperCache.CreateIfNotExist(matched.Item2, matched.Item4));
            generator.Emit(OpCodes.Callvirt, matched.Item3.SetMethod!);
        }

        generator.Emit(OpCodes.Ret);
        return new MethodMetaData(typeof(Utilities.MapEntityProperties<,,>).MakeGenericType(sourceType, targetType, typeof(int)), method.Name);
    }

    public MethodMetaData? BuildUpEntityListPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        IList<(PropertyInfo, Type, PropertyInfo, Type)> matchedProperties)
    {
        var methodName = BuildMapperMethodName(MapListPropertiesMethod, sourceType, targetType);
        var method = BuildMethod(
            methodName,
            new[] { typeof(IListPropertyMapper<int>), sourceType, targetType, typeof(INewTargetTracker<int>), typeof(EntityPropertyMappingData?) },
            typeof(void));
        var generator = method.GetILGenerator();

        foreach (var match in matchedProperties)
        {
            // now it's made sure that mapper between list items exists, emit the list property mapping code
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Callvirt, match.Item3.GetMethod!);
            var jumpLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Brtrue_S, jumpLabel);
            if (match.Item3.SetMethod != default)
            {
                generator.Emit(OpCodes.Ldarg_2);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, _listTypeConstructorCache.CreateIfNotExist(match.Item3.PropertyType, match.Item4));
                generator.Emit(OpCodes.Callvirt, match.Item3.SetMethod!);
            }
            else
            {
                generator.Emit(OpCodes.Ldstr, $"Entity type: {targetType}, property name: {match.Item3.Name}, value is empty and the property doesn't have a setter.");
                generator.Emit(OpCodes.Newobj, MissingSetting);
                generator.Emit(OpCodes.Throw);
            }

            generator.MarkLabel(jumpLabel);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, match.Item1.GetMethod!);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Callvirt, match.Item3.GetMethod!);
            generator.Emit(OpCodes.Ldarg_3);
            generator.Emit(OpCodes.Ldarg, 4);
            generator.Emit(OpCodes.Callvirt, _entityListPropertyMapperCache.CreateIfNotExist(match.Item2, match.Item4));
        }

        generator.Emit(OpCodes.Ret);

        return new MethodMetaData(typeof(Utilities.MapListProperties<,,>).MakeGenericType(sourceType, targetType, typeof(int)), method.Name);
    }

    public MethodMetaData BuildUpKeyEqualComparerMethod(
        KeyType keyType,
        Type sourceType,
        Type targetType,
        PropertyInfo sourceKeyProperty,
        PropertyInfo targetKeyProperty)
    {
        return BuildUpKeyEqualComparerMethod(sourceType, targetType, sourceKeyProperty, targetKeyProperty, keyType == KeyType.Id ? CompareIdMethod : CompareConcurrencyTokenMethod);
    }

    public MethodMetaData BuildUpGetIdMethod(KeyType keyType, Type type, PropertyInfo identityProperty)
    {
        return BuildUpGetKeyMethod(type, identityProperty, GetId);
    }

    public MethodMetaData BuildUpKeyIsEmptyMethod(KeyType keyType, Type type, PropertyInfo identityProperty)
    {
        return BuildUpKeyIsEmptyMethod(type, identityProperty, keyType == KeyType.Id ? IdEmpty : ConcurrencyTokenEmpty);
    }

    private static string GetTypeName(Type type)
    {
        return $"{type.Namespace}_{type.Name}".Replace(".", "_").Replace("`", "_");
    }

    private static string BuildMethodName(char prefix, Type entityType)
    {
        return $"_{prefix}__{GetTypeName(entityType)}";
    }

    private static string BuildMapperMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{GetTypeName(sourceType)}__MapTo__{GetTypeName(targetType)}";
    }

    private static string BuildPropertyCompareMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{GetTypeName(sourceType)}__CompareTo__{GetTypeName(targetType)}";
    }

    private MethodMetaData BuildUpKeyEqualComparerMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo sourceProperty,
        PropertyInfo targetProperty,
        char prefix)
    {
        var methodName = BuildPropertyCompareMethodName(prefix, sourceType, targetType);
        var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IScalarTypeConverter) }, typeof(bool));
        var generator = method.GetILGenerator();
        GenerateScalarPropertyEqualIL(generator, sourceProperty, targetProperty);
        return new MethodMetaData(typeof(Utilities.ScalarPropertiesAreEqual<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    private MethodMetaData BuildUpGetKeyMethod(Type type, PropertyInfo keyProperty, char prefix)
    {
        var methodName = BuildMethodName(prefix, type);
        var method = BuildMethod(methodName, new[] { type }, typeof(object));
        var generator = method.GetILGenerator();

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, keyProperty.GetMethod!);
        var keyPropertyType = keyProperty.PropertyType;
        if (keyPropertyType.IsValueType)
        {
            generator.Emit(OpCodes.Box, keyPropertyType);
        }

        generator.Emit(OpCodes.Ret);

        return new MethodMetaData(typeof(Utilities.GetScalarProperty<>).MakeGenericType(type), method.Name);
    }

    private MethodMetaData BuildUpKeyIsEmptyMethod(Type type, PropertyInfo keyProperty, char prefix)
    {
        var methodName = BuildMethodName(prefix, type);
        var method = BuildMethod(methodName, new[] { type }, typeof(bool));
        var generator = method.GetILGenerator();
        GenerateScalarPropertyEmptyIL(generator, keyProperty);
        return new MethodMetaData(typeof(Utilities.ScalarPropertyIsEmpty<>).MakeGenericType(type), method.Name);
    }

    private MethodBuilder BuildMethod(string methodName, Type[] parameterTypes, Type returnType)
    {
        var methodBuilder = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static);
        methodBuilder.SetParameters(parameterTypes);
        methodBuilder.SetReturnType(returnType);

        return methodBuilder;
    }

    private void GenerateScalarPropertyValueAssignmentIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
        var sourcePropertyType = sourceProperty.PropertyType;
        var targetPropertyType = targetProperty.PropertyType;
        var needToConvert = sourcePropertyType != targetPropertyType;
        generator.Emit(OpCodes.Ldarg_1);
        if (needToConvert)
        {
            generator.Emit(OpCodes.Ldarg_2);
        }

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        if (needToConvert)
        {
            generator.Emit(OpCodes.Callvirt, _scalarPropertyConverterCache.CreateIfNotExist(sourcePropertyType, targetPropertyType));
        }

        generator.Emit(OpCodes.Callvirt, targetProperty.SetMethod!);
    }

    private void GenerateScalarPropertyEmptyIL(ILGenerator generator, PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        var methodInfo = _isDefaultValueCache.GetMethodFor(propertyType);
        if (methodInfo != default)
        {
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, property.GetMethod!);
            generator.Emit(OpCodes.Call, methodInfo);
        }
        else
        {
            if (propertyType.IsValueType)
            {
                if (propertyType.IsPrimitive)
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Callvirt, property.GetMethod!);
                    generator.Emit(OpCodes.Ldc_I4_0);
                    generator.Emit(OpCodes.Ceq);
                }
                else
                {
                    generator.DeclareLocal(propertyType);
                    generator.DeclareLocal(propertyType);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Callvirt, property.GetMethod!);
                    generator.Emit(OpCodes.Stloc_0);
                    generator.Emit(OpCodes.Ldloca_S, 0);
                    generator.Emit(OpCodes.Ldloca_S, 1);
                    generator.Emit(OpCodes.Initobj, propertyType);
                    generator.Emit(OpCodes.Ldloc_1);
                    generator.Emit(OpCodes.Box, propertyType);
                    generator.Emit(OpCodes.Constrained, propertyType);
                    generator.Emit(OpCodes.Callvirt, ObjectEqual);
                }
            }
            else
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, property.GetMethod!);
                generator.Emit(OpCodes.Call, _isDefaultValueCache.DefaultMethod);
            }
        }

        generator.Emit(OpCodes.Ret);
    }

    private void GenerateScalarPropertyEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
        var sourcePropertyType = sourceProperty.PropertyType;
        var targetPropertyType = targetProperty.PropertyType;
        var equalMethod = _areEqualCache.GetMethodFor(targetPropertyType);
        if (sourcePropertyType != targetPropertyType)
        {
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
            generator.Emit(OpCodes.Callvirt, _scalarPropertyConverterCache.CreateIfNotExist(sourcePropertyType, targetPropertyType));
            if (equalMethod == default && targetPropertyType.IsValueType)
            {
                generator.Emit(OpCodes.Box, targetPropertyType);
            }
        }
        else
        {
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
            if (equalMethod == default && sourcePropertyType.IsValueType)
            {
                generator.Emit(OpCodes.Box, sourcePropertyType);
            }
        }

        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        if (equalMethod == default && targetPropertyType.IsValueType)
        {
            generator.Emit(OpCodes.Box, targetPropertyType);
        }

        if (equalMethod != default)
        {
            generator.Emit(OpCodes.Call, equalMethod);
        }
        else
        {
            generator.Emit(OpCodes.Call, _areEqualCache.DefaultMethod);
        }

        generator.Emit(OpCodes.Ret);
    }
}

public interface IScalarTypeMethodCache
{
    MethodInfo DefaultMethod { get; }

    MethodInfo? GetMethodFor(Type type);
}

internal class ScalarTypeMethodCache : IScalarTypeMethodCache
{
    private readonly MethodInfo _generalMethod;

    private readonly Dictionary<Type, MethodInfo> _cache;

    public ScalarTypeMethodCache(Type type, string methodName, Type[] parameters)
    {
        _cache = type.GetMethods(BindingFlags.Public | BindingFlags.Static).ToDictionary(m => m.GetParameters()[0].ParameterType, m => m);
        _generalMethod = type.GetMethod(methodName, parameters)!;
    }

    public MethodInfo DefaultMethod => _generalMethod;

    public MethodInfo? GetMethodFor(Type type) => _cache.TryGetValue(type, out var result) ? result : default;
}

public static class ScalarTypeIsDefaultValueMethods
{
    public static bool IsDefaultValue(byte x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(byte? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(short x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(ushort x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(short? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(ushort? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(int x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(uint x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(int? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(uint? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(long x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(ulong x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(long? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(ulong? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(string? x)
    {
        return string.IsNullOrEmpty(x);
    }

    public static bool IsDefaultValue(byte[]? x)
    {
        return x == default || !x.Any();
    }

    public static bool IsDefaultValue(Guid x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(Guid? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(object? x)
    {
        return x == default;
    }
}

public static class ScalarTypeEqualMethods
{
    public static bool AreEqual(byte x, byte y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(byte? x, byte? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(short x, short y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(ushort x, ushort y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(short? x, short? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(ushort? x, ushort? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(int x, int y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(uint x, uint y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(int? x, int? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(uint? x, uint? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(long x, long y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(ulong x, ulong y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(long? x, long? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(ulong? x, ulong? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(string? x, string? y)
    {
        return string.Equals(x, y);
    }

    public static bool AreEqual(byte[]? x, byte[]? y)
    {
        return x != default && y != default && Enumerable.SequenceEqual(x, y);
    }

    public static bool AreEqual(Guid x, Guid y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(Guid? x, Guid? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(object? x, object? y)
    {
        return x != default && y != default && Equals(x, y);
    }
}