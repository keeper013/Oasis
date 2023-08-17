﻿namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Reflection.Emit;

internal sealed class MapperBuilder : IMapperBuilder
{
    private readonly MapperRegistry _mapperRegistry;
    private readonly bool _throwForRedundantConfiguration;

    public MapperBuilder(string assemblyName, IMapperBuilderConfiguration? configuration)
    {
        var name = new AssemblyName($"{assemblyName}.Oasis.EntityFrameworkCore.Mapper.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _mapperRegistry = new (module, configuration);
        _throwForRedundantConfiguration = configuration?.ThrowForRedundantConfiguration ?? true;
    }

    public IMapper Build()
    {
        var type = _mapperRegistry.Build();
        var scalarTypeConverter = _mapperRegistry.MakeScalarTypeConverter();
        var listTypeConstructor = _mapperRegistry.MakeListTypeConstructor(type);
        var lookup = _mapperRegistry.MakeMapperSetLookUp(type);
        var entityHandler = _mapperRegistry.MakeEntityHandler(type, scalarTypeConverter);
        var recursiveMappingContextFactory = _mapperRegistry.MakeRecursiveMappingContextFactory(type, entityHandler, scalarTypeConverter);
        var dependentPropertyManager = _mapperRegistry.MakeDependentPropertyManager();
        var keepUnmatchedManager = _mapperRegistry.KeepUnmatchedManager;
        var mapToDatabaseTypeManager = _mapperRegistry.MakeMapToDatabaseTypeManager();

        // release some memory ahead
        _mapperRegistry.Clear();

        return new Mapper(scalarTypeConverter, listTypeConstructor, lookup, entityHandler, dependentPropertyManager, keepUnmatchedManager, mapToDatabaseTypeManager, recursiveMappingContextFactory);
    }

    public IMapperBuilder Register<TSource, TTarget>()
        where TSource : class
        where TTarget : class
    {
        _mapperRegistry.Register(typeof(TSource), typeof(TTarget));
        return this;
    }

    public IMapperBuilder RegisterTwoWay<TSource, TTarget>()
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        _mapperRegistry.Register(sourceType, targetType);
        if (sourceType != targetType)
        {
            _mapperRegistry.Register(targetType, sourceType);
        }

        return this;
    }

    IMapperBuilder IMapperBuilder.WithFactoryMethod<TList, TItem>(Func<TList> factoryMethod, bool throwIfRedundant)
    {
        _mapperRegistry.WithFactoryMethod(typeof(TList), typeof(TItem), factoryMethod, throwIfRedundant);
        return this;
    }

    public IMapperBuilder WithFactoryMethod<TEntity>(Func<TEntity> factoryMethod, bool throwIfRedundant = false)
        where TEntity : class
    {
        _mapperRegistry.WithFactoryMethod(typeof(TEntity), factoryMethod, throwIfRedundant);
        return this;
    }

    public IMapperBuilder WithScalarConverter<TSource, TTarget>(Func<TSource, TTarget> func, bool throwIfRedundant = false)
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (sourceType == targetType)
        {
            throw new SameTypeException(targetType);
        }

        _mapperRegistry.WithScalarConverter(typeof(TSource), typeof(TTarget), func, throwIfRedundant);
        return this;
    }

    public IEntityConfiguration<TEntity> Configure<TEntity>()
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (_throwForRedundantConfiguration && _mapperRegistry.IsConfigured(type))
        {
            throw new RedundantConfigurationException(type);
        }

        return new EntityConfigurationBuilder<TEntity>(this);
    }

    public ICustomTypeMapperConfiguration<TSource, TTarget> Configure<TSource, TTarget>()
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (_throwForRedundantConfiguration && _mapperRegistry.IsConfigured(sourceType, targetType))
        {
            throw new RedundantConfigurationException(sourceType, targetType);
        }

        return new CustomTypeMapperBuilder<TSource, TTarget>(this);
    }

    internal void Configure<TEntity>(IEntityConfiguration configuration)
    {
        if (configuration.IdentityPropertyName == null && configuration.ConcurrencyTokenPropertyName == null && configuration.ExcludedProperties == null
                && configuration.KeepUnmatchedProperties == null && configuration.DependentProperties == null)
        {
            throw new EmptyConfigurationException(typeof(IEntityConfiguration));
        }

        var properties = typeof(TEntity).GetProperties(Utilities.PublicInstance);
        if (!string.IsNullOrEmpty(configuration.IdentityPropertyName))
        {
            var identityProperty = properties.GetKeyProperty(configuration.IdentityPropertyName, false);
            if (identityProperty == default)
            {
                throw new MissingKeyPropertyException(typeof(TEntity), "identity", configuration.IdentityPropertyName);
            }

            if (!string.IsNullOrEmpty(configuration.ConcurrencyTokenPropertyName) && properties.GetKeyProperty(configuration.IdentityPropertyName, false) == null)
            {
                throw new MissingKeyPropertyException(typeof(TEntity), "concurrency token", configuration.ConcurrencyTokenPropertyName);
            }
        }

        if (configuration.ExcludedProperties != null && configuration.ExcludedProperties.Any())
        {
            foreach (var propertyName in configuration.ExcludedProperties)
            {
                if (!properties.Any(p => string.Equals(p.Name, propertyName)))
                {
                    throw new UselessExcludeException(typeof(TEntity), propertyName);
                }
            }

            if (configuration.KeepUnmatchedProperties != null)
            {
                var excluded = configuration.KeepUnmatchedProperties.FirstOrDefault(p => configuration.ExcludedProperties.Contains(p));
                if (!string.IsNullOrEmpty(excluded))
                {
                    throw new KeepUnmatchedPropertyExcludedException(typeof(TEntity), excluded);
                }
            }
        }

        _mapperRegistry.Configure(typeof(TEntity), configuration);
    }

    internal void Configure<TSource, TTarget>(ICustomTypeMapperConfiguration configuration)
    {
        if (configuration.CustomPropertyMapper == null && configuration.KeepUnmatchedProperties == null && configuration.ExcludedProperties == null && configuration.MapToDatabaseType == null)
        {
            throw new EmptyConfigurationException(typeof(ICustomTypeMapperConfiguration));
        }

        if (configuration.ExcludedProperties != null)
        {
            if (configuration.CustomPropertyMapper != null)
            {
                var excluded = configuration.CustomPropertyMapper.MappedTargetProperties.FirstOrDefault(p => configuration.ExcludedProperties.Contains(p.Name));
                if (excluded != null)
                {
                    throw new CustomMappingPropertyExcludedException(typeof(TSource), typeof(TTarget), excluded.Name);
                }
            }

            if (configuration.KeepUnmatchedProperties != null)
            {
                var excluded = configuration.KeepUnmatchedProperties.FirstOrDefault(p => configuration.ExcludedProperties.Contains(p));
                if (!string.IsNullOrEmpty(excluded))
                {
                    throw new KeepUnmatchedPropertyExcludedException(typeof(TSource), typeof(TTarget), excluded);
                }
            }
        }

        if (configuration.CustomPropertyMapper != null)
        {
            if (configuration.KeepUnmatchedProperties != null)
            {
                var keepUnmatched = configuration.CustomPropertyMapper.MappedTargetProperties.FirstOrDefault(p => configuration.KeepUnmatchedProperties.Contains(p.Name));
                if (keepUnmatched != null)
                {
                    throw new CustomMappingPropertyKeepUnmatchedException(typeof(TSource), typeof(TTarget), keepUnmatched.Name);
                }
            }
        }

        _mapperRegistry.Configure(typeof(TSource), typeof(TTarget), configuration);
    }
}
