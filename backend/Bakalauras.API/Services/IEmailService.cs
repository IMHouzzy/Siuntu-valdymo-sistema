using System.Threading.Channels;
using Resend;

namespace Bakalauras.API.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
    Task SendWithAttachmentAsync(string to, string subject, string htmlBody,
                                 string attachmentPath, string attachmentName);
    Task SendWithAttachmentsAsync(string to, string subject, string htmlBody,
                                  IEnumerable<(string path, string name)> attachments);
}

public sealed class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _log;
    public NoOpEmailService(ILogger<NoOpEmailService> log) => _log = log;

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        _log.LogWarning("Email skipped (no API key configured): {Subject} → {To}", subject, to);
        return Task.CompletedTask;
    }

    public Task SendWithAttachmentAsync(string to, string subject, string htmlBody,
                                        string attachmentPath, string attachmentName)
        => SendAsync(to, subject, htmlBody);

    public Task SendWithAttachmentsAsync(string to, string subject, string htmlBody,
                                         IEnumerable<(string path, string name)> attachments)
        => SendAsync(to, subject, htmlBody);
}

public class ResendEmailService : IEmailService
{
    private readonly EmailBackgroundQueue _queue;
    private readonly ILogger<ResendEmailService> _log;

    public ResendEmailService(EmailBackgroundQueue queue, ILogger<ResendEmailService> log)
    {
        _queue = queue;
        _log = log;
    }

    public Task SendWithAttachmentsAsync(string to, string subject, string htmlBody,
                                         IEnumerable<(string path, string name)> attachments)
    {
        _queue.Queue(new QueuedEmail(
            to,
            subject,
            htmlBody,
            attachments.ToArray()));

        _log.LogInformation("Email queued for {To} - {Subject}", to, subject);
        return Task.CompletedTask;
    }

    public Task SendAsync(string to, string subject, string htmlBody)
        => SendWithAttachmentsAsync(to, subject, htmlBody, Array.Empty<(string path, string name)>());

    public Task SendWithAttachmentAsync(string to, string subject, string htmlBody,
                                        string attachmentPath, string attachmentName)
        => SendWithAttachmentsAsync(to, subject, htmlBody,
            new[] { (attachmentPath, attachmentName) });
}

public sealed record QueuedEmail(
    string To,
    string Subject,
    string HtmlBody,
    IReadOnlyCollection<(string path, string name)> Attachments);

public sealed class EmailBackgroundQueue
{
    private readonly Channel<QueuedEmail> _queue = Channel.CreateUnbounded<QueuedEmail>();

    public void Queue(QueuedEmail email)
        => _queue.Writer.TryWrite(email);

    public IAsyncEnumerable<QueuedEmail> ReadAllAsync(CancellationToken cancellationToken)
        => _queue.Reader.ReadAllAsync(cancellationToken);
}

public sealed class ResendEmailBackgroundWorker : BackgroundService
{
    private readonly EmailBackgroundQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _cfg;
    private readonly ILogger<ResendEmailBackgroundWorker> _log;

    public ResendEmailBackgroundWorker(
        EmailBackgroundQueue queue,
        IServiceScopeFactory scopeFactory,
        IConfiguration cfg,
        ILogger<ResendEmailBackgroundWorker> log)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _cfg = cfg;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in _queue.ReadAllAsync(stoppingToken))
        {
            // Create a short-lived scope per email so scoped services work fine
            await using var scope = _scopeFactory.CreateAsyncScope();
            var resend = scope.ServiceProvider.GetRequiredService<IResend>();
            await SendCoreAsync(email, resend, stoppingToken);
        }
    }

    private async Task SendCoreAsync(QueuedEmail email, IResend resend, CancellationToken cancellationToken)
    {
        var section = _cfg.GetSection("EmailSettings");
        var fromName = section["FromName"] ?? "System";
        var fromAddr = section["FromAddress"]
            ?? throw new InvalidOperationException("EmailSettings:FromAddress missing");

        var message = new EmailMessage
        {
            From = string.IsNullOrWhiteSpace(fromName) ? fromAddr : $"{fromName} <{fromAddr}>",
            Subject = email.Subject,
            HtmlBody = email.HtmlBody,
        };
        message.To.Add(email.To);

        try
        {
            message.Attachments = await BuildAttachmentsAsync(email.Attachments, cancellationToken);

            var response = await resend.EmailSendAsync(message, cancellationToken);
            _log.LogInformation("Email sent to {To} - {Subject}. Resend id: {EmailId}",
                email.To, email.Subject, response.Content);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Resend failed for {To} - {Subject}", email.To, email.Subject);
        }
    }


    private static async Task<List<EmailAttachment>?> BuildAttachmentsAsync(
        IEnumerable<(string path, string name)> attachments,
        CancellationToken cancellationToken)
    {
        var result = new List<EmailAttachment>();

        foreach (var (path, name) in attachments)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                continue;

            var attachment = await EmailAttachment.FromAsync(path, cancellationToken);

            if (!string.IsNullOrWhiteSpace(name))
                attachment.Filename = name;

            result.Add(attachment);
        }

        return result.Count == 0 ? null : result;
    }
}
