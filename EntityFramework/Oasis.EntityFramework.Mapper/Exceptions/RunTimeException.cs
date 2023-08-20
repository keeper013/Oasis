﻿namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class KeyPropertyMissingException : EfMapperException
{
    public KeyPropertyMissingException(Type type, string propertyName)
        : base($"Type {type.Name} doesn't have a proper property for {propertyName}.")
    {
    }

    public KeyPropertyMissingException(Type sourceType, Type targetType, string propertyName)
        : base($"Either type {sourceType.Name} or {targetType.Name} doesn't have a proper property for {propertyName}.")
    {
    }
}