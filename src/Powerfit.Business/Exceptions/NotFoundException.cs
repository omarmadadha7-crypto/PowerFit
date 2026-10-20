using System;

namespace Powerfit.Business.Exceptions;

/// <summary>The record does not exist (or does not belong to the caller). Map to HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string entity, object key)
        : base($"{entity} '{key}' was not found.") { }
}
