using FastEndpoints;
using Polly.Registry;
using Propelle.InterviewChallenge.Application;
using Propelle.InterviewChallenge.Application.Domain;
using Propelle.InterviewChallenge.Application.Domain.Events;

namespace Propelle.InterviewChallenge.Endpoints
{
    public static class MakeDeposit
    {
        public class Request
        {
            public Guid UserId { get; set; }

            public decimal Amount { get; set; }
        }

        public class Response
        {
            public Guid DepositId { get; set; }
        }

        public class Endpoint : Endpoint<Request, Response>
        {
            private readonly PaymentsContext _paymentsContext;
            private readonly Application.EventBus.IEventBus _eventBus;
            private readonly ResiliencePipelineProvider<string> _pipelineProvider;

            public Endpoint(
                PaymentsContext paymentsContext,
                Application.EventBus.IEventBus eventBus,
                ResiliencePipelineProvider<string> pipelineProvider)
            {
                _paymentsContext = paymentsContext;
                _eventBus = eventBus;
                _pipelineProvider = pipelineProvider;
            }

            public override void Configure()
            {
                Post("/api/deposits/{UserId}");
            }

            public override async Task HandleAsync(Request req, CancellationToken ct)
            {
                var deposit = new Deposit(req.UserId, req.Amount);
                _paymentsContext.Deposits.Add(deposit);

                // Save to DB with retry (with exponential backoff)
                var retryOnException = _pipelineProvider.GetPipeline(Constants.DefaultRetryPipelineID);
                await retryOnException.ExecuteAsync(
                    async (ct) => await _paymentsContext.SaveChangesAsync(ct),
                    ct);

                await _eventBus.Publish(new DepositMade
                {
                    Id = deposit.Id
                });

                await SendAsync(new Response { DepositId = deposit.Id }, 201, ct);
            }
        }
    }
}
