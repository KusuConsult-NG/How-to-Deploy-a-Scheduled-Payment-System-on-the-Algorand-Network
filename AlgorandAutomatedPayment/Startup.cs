using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using AlgorandAutomatedPayment.Services;
using Hangfire;
using HangfireBasicAuthenticationFilter;

namespace AlgorandAutomatedPayment
{
    public class Startup
    {
        private static IPayments paymentService;
        private readonly Job jobscheduler = new Job(paymentService);
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hangfire", Version = "v1" });
            });
            #region Configure Hangfire
            services.AddHangfire(c => c.UseSqlServerStorage(Configuration.GetConnectionString("myconn")));
            GlobalConfiguration.Configuration.UseSqlServerStorage(Configuration.GetConnectionString("myconn")).WithJobExpirationTimeout(TimeSpan.FromDays(7));
            #endregion
            #region Services Injection
            services.AddTransient<IPayments, Payments>();
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hangfire v1"));
            }

            #region Configure Hangfire
            app.UseHangfireServer();

            //Basic Authentication added to access the Hangfire Dashboard
            app.UseHangfireDashboard("/hangfire", new DashboardOptions()
            {
                AppPath = null,
                DashboardTitle = "Hangfire Dashboard",
                Authorization = new[]{
                new HangfireCustomBasicAuthenticationFilter{
                    User = Configuration.GetSection("HangfireCredentials:UserName").Value,
                    Pass = Configuration.GetSection("HangfireCredentials:Password").Value
                }
            },
            }); ;
            #endregion

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            #region Job Scheduling Tasks
            // Recurring One Time Payment Job for every 5 min
            recurringJobManager.AddOrUpdate("Insert Tasks : Runs Every 5 Min", () => jobscheduler.OneTimePaymentJobAsync(), "*/5 * * * *");

            // Recurring Atomic Transfer Job for every 5 min
            recurringJobManager.AddOrUpdate("Insert Tasks : Runs Every 5 Min", () => jobscheduler.GroupedPayments(), "*/5 * * * *");

            //Fire and forget job 
            var jobId = backgroundJobClient.Enqueue(() => jobscheduler.OneTimePaymentJobAsync());

            //Continous Job
            backgroundJobClient.ContinueJobWith(jobId, () => jobscheduler.GroupedPayments());

            //Schedule Job / Delayed Job

            backgroundJobClient.Schedule(() => jobscheduler.GroupedPayments(), TimeSpan.FromDays(29));
            #endregion
        }
    }
}
