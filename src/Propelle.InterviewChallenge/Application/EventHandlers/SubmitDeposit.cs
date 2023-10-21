using Polly.Registry;
using Propelle.InterviewChallenge.Application.Domain;
using Propelle.InterviewChallenge.Application.Domain.Events;
using Propelle.InterviewChallenge.EventHandling;

namespace Propelle.InterviewChallenge.Application.EventHandlers
{
    public class SubmitDeposit : IEventHandler<DepositMade>
    {
        private readonly PaymentsContext _context;
        private readonly ISmartInvestClient _smartInvestClient;
        private readonly ResiliencePipelineProvider<string> _pipelineProvider;

        public SubmitDeposit(PaymentsContext context,
            ISmartInvestClient smartInvestClient,
            ResiliencePipelineProvider<string> pipelineProvider)
        {
            _context = context;
            _smartInvestClient = smartInvestClient;
            _pipelineProvider = pipelineProvider;
        }

        public async Task Handle(DepositMade @event)
        {
            var deposit = await _context.Deposits.FindAsync(@event.Id);

            // Save to DB with retry (with exponential backoff)
            var retryOnException = _pipelineProvider.GetPipeline(Constants.DefaultRetryPipelineID);
            await retryOnException.ExecuteAsync(async (ct) =>
                await _smartInvestClient.SubmitDeposit(deposit.UserId, deposit.Amount));
        }
    }
}
