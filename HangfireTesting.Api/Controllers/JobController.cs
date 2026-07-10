using Hangfire;
using HangfireTesting.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HangfireTesting.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        // Fire-and-forget: runs once, as soon as possible
        [HttpPost("welcome")]
        public IActionResult Welcome(string email)
        {
            BackgroundJob.Enqueue<JobService>(x=>x.SendWelcomeEmail(email));
            return Ok("Welcome email queued.");
        }

        // Delayed: runs once, after a delay
        [HttpPost("reminder")]
        public IActionResult Reminder(string email)
        {
            BackgroundJob.Schedule<JobService>(
                x => x.SendReminder(email),
                TimeSpan.FromMinutes(1));

            return Ok("Reminder scheduled in 1 minute.");

            // Note: Hangfire's scheduler only checks for due jobs every 15 seconds (default)
        }

        // Recurring: runs on a cron schedule
        [HttpPost("recurring")]
        public IActionResult Recurring()
        {
            RecurringJob.AddOrUpdate<JobService>("nightly-cleanup", x => x.NightlyCleanup(), Cron.Minutely);

            return Ok("Recurring Job registered.");
        }

        // Continuation job
        [HttpPost("continuous_jobs")]
        public IActionResult Continuous_Jobs()
        {
            var job1 = BackgroundJob.Enqueue<JobService>(x => x.SampleJob("Job A"));

            BackgroundJob.ContinueJobWith<JobService>(job1, x => x.SampleJob("Job B"));

            return Ok("Job A has started. Job B will continue after.");
        }
    }
}
