using FastEndpoints;
using Propelle.InterviewChallenge.Application;
using Propelle.InterviewChallenge.Application.Domain;
using Propelle.InterviewChallenge.Application.Domain.Events;
using Propelle.InterviewChallenge.Application.EventBus;
using Propelle.InterviewChallenge.Application.EventHandlers;
using Propelle.InterviewChallenge.EventHandling;
using Polly;
using Polly.Retry;
using Polly.Telemetry;
using Propelle.InterviewChallenge.Application.Telemetry;

namespace Propelle.InterviewChallenge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<PaymentsContext>();
            builder.Services.AddSingleton<ISmartInvestClient, SmartInvestClient>();
            builder.Services.AddSingleton<InMemoryEventExchange>();
            builder.Services.AddSingleton<Application.EventBus.IEventBus, SimpleEventBus>();
            builder.Services.AddTransient<EventHandling.IEventHandler<DepositMade>, SubmitDeposit>();
            builder.Services.AddFastEndpoints();


            // Retry pipeline service

            // Setup logging for retries
            var telemetryOptions = new TelemetryOptions
            {
                LoggerFactory = LoggerFactory.Create(builder => builder.AddConsole())
            };
            telemetryOptions.TelemetryListeners.Add(new SimpleTelemetryListener());

            // Add retry service to service provider
            builder.Services.AddResiliencePipeline(Constants.DefaultRetryPipelineID, builder =>
            {
                builder.AddRetry(new RetryStrategyOptions
                    {
                        ShouldHandle = new PredicateBuilder().Handle<TransientException>(),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        MaxRetryAttempts = 50, 
                        Delay = TimeSpan.FromSeconds(0.0001), // Low base delay for testing, should increase if issue due to rate limiting
                    })
                    .AddTimeout(TimeSpan.FromSeconds(10))
                    .ConfigureTelemetry(telemetryOptions);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseFastEndpoints(x =>
            {
                /* Allow anonymous access globally for the purposes of this exercise */
                x.Endpoints.Configurator = epd => epd.AllowAnonymous();
            });
            app.Run();
        }
    }
}