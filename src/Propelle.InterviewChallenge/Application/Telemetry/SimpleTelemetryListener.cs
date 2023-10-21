using Polly.Telemetry;

namespace Propelle.InterviewChallenge.Application.Telemetry
{
    /// <summary>
    /// Store Telemetry data when retry occurs.
    /// </summary>
    internal class SimpleTelemetryListener : TelemetryListener
    {
        public override void Write<TResult, TArgs>(in TelemetryEventArguments<TResult, TArgs> args)
        {
            // In prod, this would write to telemetry service
            Console.WriteLine($"Transient error occured for: {args.Event.EventName}");
        }
    }
}