namespace Frever.Shared.MainDb.Entities;

public class AiLlmPrompt
{
    public long Id { get; set; }
    public string Key { get; set; }
    public string Prompt { get; set; }
}

public class AiOpenAiKey
{
    public long Id { get; set; }
    public string ApiKey { get; set; }
}

public class AiOpenAiAgent
{
    public long Id { get; set; }
    public string Key { get; set; }
    public string Agent { get; set; }
}