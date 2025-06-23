using System.Threading.Tasks;

namespace Common.Infrastructure.EmailSending;

public interface IEmailSendingService
{
    Task SendEmail(SendEmailParams parameters);
}