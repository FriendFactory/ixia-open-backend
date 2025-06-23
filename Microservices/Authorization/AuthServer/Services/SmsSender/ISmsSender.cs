using System.Threading.Tasks;

namespace AuthServer.Services.SmsSender;

public interface ISmsSender
{
    Task SendMessage(string phoneNumber, string text);

    Task<string> FormatPhoneNumber(string phoneNumber);
}