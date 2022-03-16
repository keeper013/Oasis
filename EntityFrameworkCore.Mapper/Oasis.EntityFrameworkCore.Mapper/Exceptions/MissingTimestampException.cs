﻿namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class MissingTimestampException : Exception
{
    private readonly Type _type;
    private readonly object _id;

    public MissingTimestampException(Type type, object id)
    {
        _type = type;
        _id = id;
    }

    public override string Message => $"Entity of (type: id) ({_type}: {_id}) is without a timestamp.";
}
