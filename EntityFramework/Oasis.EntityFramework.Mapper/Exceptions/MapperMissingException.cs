﻿namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class MapperMissingException : EfMapperException
{
    public MapperMissingException(Type sourceType, Type targetType)
        : base($"Entity mapper from type {sourceType} to {targetType} hasn't been registered yet.")
    {
    }

    public MapperMissingException(Type targetType)
        : base($"Type {targetType} hasn't been register as a mapping target.")
    {
    }
}
