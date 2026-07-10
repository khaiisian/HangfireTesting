namespace HangfireTesting.Api.Services;

public class JobService
{
    private readonly ILogger<JobService> _logger;

    public JobService(ILogger<JobService> logger)
    {
        _logger = logger;
    }

    public void SendWelcomeEmail (string email)
    {
        _logger.LogInformation("Sending welcome mail to {Email}", email);
    }

    public void SendReminder (string email)
    {
        _logger.LogInformation("Sending reminder mail to {Email}", email);
    }

    public void NightlyCleanup()
    {
        _logger.LogInformation("Running nightly cleanup at {Time}", DateTime.Now);
    }

    public void SampleJob(string jobName)
    {
        _logger.LogInformation("{JobName} started running", jobName);
    }
}
