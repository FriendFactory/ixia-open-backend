using System.Collections.Generic;

namespace Frever.ClientService.Contract.Metadata;

public class PromptInput
{
    public string[] Keys { get; set; }
}

public class PromptDataDto
{
    public Dictionary<string, string> Prompts { get; set; }
}