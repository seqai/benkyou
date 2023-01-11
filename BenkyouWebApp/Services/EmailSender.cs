using BenkyouWebApp.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BenkyouWebApp.Services;

public class EmailSender : IEmailSender
{
    public EmailServiceConfiguration _configuration;

    private readonly ILogger _logger;

    public EmailSender(IOptions<EmailServiceConfiguration> optionsAccessor,
        ILogger<EmailSender> logger)
    {
        _configuration = optionsAccessor.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        if (string.IsNullOrEmpty(_configuration.SendGridApiKey))
        {
            _logger.LogWarning("SendGrid API key is not configured. Email was not sent.");
            return;
        }
        await Execute(_configuration.SendGridApiKey, subject, message, toEmail);
    }

    public async Task Execute(string apiKey, string subject, string message, string toEmail)
    {
        var client = new SendGridClient(apiKey);
        var msg = new SendGridMessage()
        {
            From = new EmailAddress("Benkyou@suchkov.io", "Benkyou"),
            Subject = subject,
            PlainTextContent = message,
            HtmlContent = message
        };
        msg.AddTo(new EmailAddress(toEmail));

        // Disable click tracking.
        // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
        msg.SetClickTracking(false, false);
        var response = await client.SendEmailAsync(msg);
        _logger.LogInformation(response.IsSuccessStatusCode
            ? $"Email to {toEmail} queued successfully!"
            : $"Failure Email to {toEmail}");
    }
}