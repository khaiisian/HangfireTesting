# Hangfire — My Study Notes

My personal notes for learning Hangfire with a Web API project.
Written in simple English so I can re-read them later.

---

## 1. What is Hangfire?

Hangfire runs **background jobs** — code that runs *outside* the normal request/response.

Instead of making the user wait while I send an email or make a report,
I hand the work to Hangfire. It runs the work in the background, saves it to
storage (so it survives restarts), and retries automatically if it fails.

**Why use it:**
- Do slow work without making the user wait.
- Run work later, or on a repeat.
- Jobs are saved, so a crash or restart does not lose them.
- Failed jobs retry by themselves.
- Built-in dashboard to *see* all jobs.

---

## 2. The 4 job types

| Type | Meaning | My method |
|------|---------|-----------|
| **Fire-and-forget** | Run once, right now | `SendWelcomeEmail` |
| **Delayed** | Run once, after a wait | `SendReminder` |
| **Recurring** | Run again and again on a schedule | `NightlyCleanup` |
| **Continuation** | Run job B only after job A finishes | report → email |

Code for each:

```csharp
// Fire-and-forget
BackgroundJob.Enqueue<JobService>(x => x.SendWelcomeEmail(email));

// Delayed (run 1 minute later)
BackgroundJob.Schedule<JobService>(x => x.SendReminder(email), TimeSpan.FromMinutes(1));

// Recurring (needs a unique ID + a schedule)
RecurringJob.AddOrUpdate<JobService>("nightly-cleanup", x => x.NightlyCleanup(), Cron.Minutely);

// Continuation (jobA first, then jobB)
var jobAId = BackgroundJob.Enqueue<JobService>(x => x.GenerateReport());
BackgroundJob.ContinueJobWith<JobService>(jobAId, x => x.EmailReport());
```

---

## 3. The 4 moving parts

- **Client** = puts jobs in.  → `BackgroundJob.Enqueue(...)` (my controller)
- **Storage** = holds jobs safely.  → in-memory OR SQL Server
- **Server** = takes jobs out and runs them.  → `AddHangfireServer()`
- **Dashboard** = lets me see jobs.  → `/hangfire`

> Important: putting a job IN and RUNNING a job are two different parts.
> The **client** drops jobs into storage. The **server** picks them up and runs them.
> No server = jobs pile up and never run.

### The lambda trick (`x => x.SendWelcomeEmail(email)`)
I am **not** calling the method here. I am *describing* the call I want Hangfire
to make later. Like writing a sticky note: "call this method with this value."
That is why the controller returns a response **instantly** — the slow work
happens on the side.

---

## 4. IMPORTANT lesson: the polling delay

**Problem I hit:** I set a job for 3 seconds, but it ran after ~15 seconds.
I set it for 1 minute, but it ran after 1 min 7 sec.

**Why:** Hangfire does NOT run a delayed job the exact moment it is due.
It only **checks** for due jobs every so often. Default = **15 seconds**.

Mailbox analogy: I put a letter marked "3:00 PM" in the mailbox. The mailman
only comes every 15 minutes. So the letter goes out on his *next* visit, not at
exactly 3:00. Hangfire is the same.

**Rule:** actual run time = my delay **+ up to one check interval (15s)**.
So "1 minute" really means "1 min to 1 min 15 sec". The extra 7 sec was just
luck of timing (always between 0 and 15 seconds).

**Fix — make it check more often:**
```csharp
builder.Services.AddHangfireServer(options =>
{
    options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
});
```
> Keep this at a few seconds in real apps. Checking every 1 sec against a real
> database all day is heavy. Fine for learning, not for production.

Note: **Fire-and-forget** jobs are NOT affected — they skip the "scheduled"
step and run almost right away.

---

## 5. Cron / recurring jobs

- `Cron.Minutely` = `"* * * * *"` (5 parts = minute level).
- There is **no** `Cron.Secondly`.
- For every second, use a **6-part** cron string (extra star = seconds):
  ```csharp
  RecurringJob.AddOrUpdate<JobService>("id", x => x.NightlyCleanup(), "* * * * * *");
  ```
- Every-second is OK for testing only. Real apps use minutes/hours/days.
- `AddOrUpdate` + a **unique ID** means calling it twice does NOT make duplicates —
  it updates the same job.

---

## 6. Storage: in-memory vs SQL Server

**In-memory** (`UseInMemoryStorage`)
- Jobs live in RAM. Restart the app = all jobs GONE.
- Great for learning. No setup.

**SQL Server** (`UseSqlServerStorage`)
- Jobs saved in a database. Survive restarts and crashes.
- What real apps use.
- Hangfire makes its own **tables** automatically... but NOT the database.

### Steps to switch to SQL Server
1. `dotnet add package Hangfire.SqlServer`
2. `dotnet add package Microsoft.Data.SqlClient`  ← easy to forget! (see errors below)
3. Add a connection string in `appsettings.json`.
4. Change the storage line:
   ```csharp
   builder.Services.AddHangfire(cfg =>
       cfg.UseSqlServerStorage(builder.Configuration.GetConnectionString("HangFireDb")));
   ```
5. **Create the database yourself first** (Hangfire does not create it).

---

## 7. Quick reference — my project

- Project: `HangfireTesting.Api`  (.NET 10)
- Dashboard: `https://localhost:7172/hangfire`
- Swagger: `https://localhost:7172/swagger` (or the openapi url)
- Endpoints:
  - `POST /api/job/welcome`   → fire-and-forget
  - `POST /api/job/reminder`  → delayed
  - `POST /api/job/recurring` → recurring

**One-line summary of each part:**
Client puts jobs in → Storage holds them → Server runs them → Dashboard shows them.
