namespace BenkyouWebApp.Configuration;

public class EmailServiceConfiguration
{
    public static string SectionName = "EmailService";

    public string SendGridApiKey { get; set; }
}