﻿namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class KeyTypeExcludedException : EfCoreMapperException
{
    public KeyTypeExcludedException(Type sourceType, Type targetType, string propertyName)
        : base($"Property {propertyName} is identity/concurrency token that can't be excluded when mapping from type {sourceType} to {targetType}.")
    {
    }
}

public sealed class CustomMappingPropertyExcludedException : EfCoreMapperException
{
    public CustomMappingPropertyExcludedException(Type sourceType, Type targetType, string propertyName)
        : base($"Property {propertyName} is customly mapped that can't be excluded when mapping from type {sourceType} to {targetType}.")
    {
    }
}

public sealed class UselessExcludeException : EfCoreMapperException
{
    public UselessExcludeException(Type type, string propertyName)
        : base($"Type {type.Name} doesn't have a valid property named {propertyName} to be excluded.")
    {
    }

    public UselessExcludeException(Type sourceType, Type targetType, string propertyName)
        : base($"When mapping from {sourceType} to {targetType}, there isn't a valid property named {propertyName} to be mapped.")
    {
    }
}

public sealed class InvalidDependentException : EfCoreMapperException
{
    public InvalidDependentException(Type type, string propertyName)
        : base($"Type {type.Name} doesn't have a valid property of class type of list of class type named {propertyName} to be a dependent property.")
    {
    }
}