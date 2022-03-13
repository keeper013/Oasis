﻿namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;

internal sealed class Mapper : IMapper
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;

    public Mapper(IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers)
    {
        _mappers = mappers;
    }

    public IMappingFromEntitiesSession CreateMappingFromEntitiesSession()
    {
        return new MappingFromEntitiesSession(_mappers);
    }

    public IMappingToEntitiesSession CreateMappingToEntitiesSession(DbContext databaseContext)
    {
        return new MappingToEntitiesSession(databaseContext, _mappers);
    }
}

internal sealed class MappingFromEntitiesSession : IMappingFromEntitiesSession
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;
    private readonly NewEntityTracker _newEntityTracker;

    public MappingFromEntitiesSession(IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers)
    {
        _newEntityTracker = new NewEntityTracker();
        _mappers = mappers;
    }

    TTarget IMappingFromEntitiesSession.Map<TSource, TTarget>(TSource source)
    {
        if (source.Id.HasValue)
        {
            if (source.Timestamp == null)
            {
                throw new MissingTimestampException(typeof(TSource), source.Id.Value);
            }
        }

        var target = new TTarget();
        new FromEntitiesRecursiveMapper(_newEntityTracker, _mappers).Map(source, target);

        return target;
    }
}

internal sealed class MappingToEntitiesSession : IMappingToEntitiesSession
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;
    private readonly DbContext _databaseContext;
    private readonly NewEntityTracker _newEntityTracker;

    public MappingToEntitiesSession(DbContext databaseContext, IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers)
    {
        _databaseContext = databaseContext;
        _newEntityTracker = new NewEntityTracker();
        _mappers = mappers;
    }

    async Task<TTarget> IMappingToEntitiesSession.Map<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>> includer)
    {
        TTarget? target;
        if (source.Id.HasValue)
        {
            target = await includer.Compile()(_databaseContext.Set<TTarget>()).SingleOrDefaultAsync(t => t.Id == source.Id);
            if (target == null)
            {
                throw new EntityNotFoundException(typeof(TTarget), source.Id.Value);
            }

            if (target.Timestamp == null)
            {
                throw new MissingTimestampException(typeof(TTarget), source.Id.Value);
            }

            if (!Enumerable.SequenceEqual(target.Timestamp!, source.Timestamp!))
            {
                throw new StaleEntityException(typeof(TTarget), source.Id.Value);
            }

            new ToEntitiesRecursiveMapper(_newEntityTracker, _mappers, _databaseContext).Map(source, target);
        }
        else
        {
            if (!_newEntityTracker.NewTargetIfNotExist<TTarget>(source.GetHashCode(), out target))
            {
                new ToEntitiesRecursiveMapper(_newEntityTracker, _mappers, _databaseContext).Map(source, target!);
                _databaseContext.Set<TTarget>().Add(target!);
            }
        }

        return target!;
    }
}

internal sealed class NewEntityTracker
{
    private readonly IDictionary<int, object> _newEntityDictionary = new Dictionary<int, object>();

    public bool NewTargetIfNotExist<TTarget>(int hashCode, out TTarget? target)
        where TTarget : class, new()
    {
        bool result = false;
        object? obj;
        target = default;
        lock (_newEntityDictionary)
        {
            result = _newEntityDictionary.TryGetValue(hashCode, out obj);
            if (!result)
            {
                target = new TTarget();
                _newEntityDictionary.Add(hashCode, target);
            }
        }

        if (result)
        {
            if (obj is TTarget)
            {
                target = obj as TTarget;
            }
            else
            {
                throw new MultipleMappingException(obj!.GetType(), typeof(TTarget));
            }
        }

        return result;
    }
}
