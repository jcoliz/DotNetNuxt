namespace DotNetNuxt.Abstractions;

/// <summary>
/// Provider of underlying email transit service for the application to be able to send emails.
/// </summary>
/// <remarks>
/// Platform-agnostic interface for sending email messages.
/// </remarks>
public interface IEmailTransportProvider
{
    /// <summary>
    /// Sends an email message to the specified recipient with the given subject and HTML content.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="subject">The subject of the email.</param>
    /// <param name="htmlMessage">The body of the email which may contain HTML tags. Do not double encode this.</param>
    Task SendEmailAsync(string email, string subject, string htmlMessage);    
}
