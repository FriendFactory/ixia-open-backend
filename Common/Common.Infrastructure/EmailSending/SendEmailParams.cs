namespace Common.Infrastructure.EmailSending;

public class SendEmailParams
{
    public string[] To { get; set; }

    public string[] Cc { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }
}