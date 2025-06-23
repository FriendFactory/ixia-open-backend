using System;
using System.Threading.Tasks;

namespace AuthServer.Services.SmsSender;

public class ConsoleSmsSender : ISmsSender
{
    public Task SendMessage(string phoneNumber, string text)
    {
        Console.WriteLine(phoneNumber);
        Console.WriteLine(text);

        return Task.CompletedTask;
    }

    public Task<string> FormatPhoneNumber(string phoneNumber)
    {
        Console.WriteLine(phoneNumber);

        return null;
    }
}