﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading;

using DMTP.lib.Auth;
using DMTP.lib.Common;
using DMTP.lib.dal.Databases.Tables;
using DMTP.lib.dal.Manager;
using DMTP.lib.Managers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

namespace DMTP.REST.Controllers
{
    public class BaseController : Controller
    {
        protected readonly DatabaseManager Database;

        protected Settings CurrentSettings;

        private readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        protected BaseController(DatabaseManager database, Settings settings)
        {
            Database = database;
            CurrentSettings = settings;
        }

        protected IActionResult RedirectNotAuthorized() => RedirectToAction("Index", "Error", new { errorMessage = "Not Authorized to do that" });

        protected ApplicationUser GetApplicationUser()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var claim = claimsIdentity?.FindFirst("ApplicationUser");

            return JsonConvert.DeserializeObject<ApplicationUser>(claim?.Value);
        }

        protected IActionResult Login(Guid userGuid, string emailAddress)
        {
            new LoginManager(Database).RecordLogin(userGuid, emailAddress, Request.HttpContext.Connection.RemoteIpAddress.ToString(), true);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userGuid.ToString()),
                new Claim("ApplicationUser", JsonConvert.SerializeObject(new UserManager(Database).GetApplicationUser(userGuid)))
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var props = new AuthenticationProperties
            {
                IsPersistent = true
            };

            Thread.CurrentPrincipal = principal;

            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props).Wait();

            return RedirectToAction("Index", "Home");
        }

        protected void SendEmail(string receiverEmail, string subject, string body)
        {
            try
            {
                var client = new SmtpClient(CurrentSettings.SMTPHostName)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(CurrentSettings.SMTPUsername, CurrentSettings.SMTPPassword),
                    Port = CurrentSettings.SMTPPortNumber
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(lib.Common.Constants.SENDER_EMAIL)
                };

                mailMessage.To.Add(receiverEmail);
                mailMessage.Body = body;
                mailMessage.Subject = subject;

                client.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Log.Error($"Failure sending email to {receiverEmail}, subject: {subject}, body: {body} due to {ex}");
            }
        }

        private string FindAvailableWorker()
        {
            var hosts = new WorkerManager(Database).GetWorkers();

            if (hosts == null)
            {
                return null;
            }

            var jobManager = new JobManager(Database);

            if (hosts.Any())
            {
                var jobs = jobManager.GetJobs().Where(a => !a.Completed).ToList();

                foreach (var host in hosts)
                {
                    if (jobs.Any(a => a.AssignedHost == host.Name))
                    {
                        continue;
                    }

                    return host.Name;
                }

                return Constants.UNASSIGNED_JOB;
            }

            return Constants.UNASSIGNED_JOB;
        }

        protected Guid? SaveJob(Jobs job)
        {
            var jobManager = new JobManager(Database);

            job.ID = Guid.NewGuid();
            job.SubmissionTime = DateTime.Now;

            if (string.IsNullOrEmpty(job.AssignedHost))
            {
                job.AssignedHost = FindAvailableWorker();
            }
            
            if (jobManager.AddJob(job))
            {
                return job.ID;
            }

            return null;
        }
    }
}