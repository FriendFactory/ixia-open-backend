using System;
using System.Collections.Generic;
using FluentValidation.Results;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content;

public class AiContentModerationException(string message, IEnumerable<ValidationFailure> errors) : Exception(message)
{
    public IEnumerable<ValidationFailure> Errors { get; private set; } = errors ?? [];
}