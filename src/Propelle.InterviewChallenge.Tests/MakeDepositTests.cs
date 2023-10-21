using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Propelle.InterviewChallenge.Application;
using Propelle.InterviewChallenge.Application.Domain;
using Propelle.InterviewChallenge.Endpoints;

namespace Propelle.InterviewChallenge.Tests
{
    public class MakeDepositTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;

        public MakeDepositTests(WebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(100)]
        public async Task MakeDeposit_XTimesSuccessfully_AlwaysProcessesXDeposits(int iterations)
        {
            var client = _factory.CreateClient();
            var requests = GenerateMakeDepositRequests(iterations);

            foreach (var request in requests)
            {
                // Simulate a customer retrying a deposit if something goes wrong
                await TryUntilSuccessful(
                    () => client.POSTAsync<MakeDeposit.Endpoint, MakeDeposit.Request, MakeDeposit.Response>(request),
                    x => x.Response.IsSuccessStatusCode);
            }

            using var scope = _factory.Services.CreateScope();
            var investrClient = scope.ServiceProvider.GetService<ISmartInvestClient>();

            var sentDeposits = investrClient.SubmittedDeposits
                .Where(x => requests.Select(x => x.UserId).Contains(x.UserId))
                .ToList();

            Assert.Equal(iterations, sentDeposits.Count);
        }

        [Theory]
        [InlineData(100)]
        public async Task MakeDeposit_XTimesSuccessfully_AlwaysStoresXDepositsInContext(int iterations)
        {
            var client = _factory.CreateClient();
            var requests = GenerateMakeDepositRequests(iterations);

            foreach (var request in requests)
            {
                // Simulate a customer retrying a deposit if something goes wrong
                await TryUntilSuccessful(
                    () => client.POSTAsync<MakeDeposit.Endpoint, MakeDeposit.Request, MakeDeposit.Response>(request),
                    x => x.Response.IsSuccessStatusCode);
            }

            // Assertions
            using var scope = _factory.Services.CreateScope();
            using var context = scope.ServiceProvider.GetService<PaymentsContext>();

            var storedDeposits = await context.Deposits
                .Where(x => requests.Select(x => x.UserId).Contains(x.UserId))
                .ToListAsync();

            Assert.Equal(iterations, storedDeposits.Count);
        }

        [Theory]
        [InlineData(100)]
        public async Task MakeDeposit_XTimesSuccessfully_ProcessedDepositsEqualStored(int iterations)
        {
            var client = _factory.CreateClient();
            var requests = GenerateMakeDepositRequests(iterations);

            foreach (var request in requests)
            {
                // Simulate a customer retrying a deposit if something goes wrong
                await TryUntilSuccessful(
                    () => client.POSTAsync<MakeDeposit.Endpoint, MakeDeposit.Request, MakeDeposit.Response>(request),
                    x => x.Response.IsSuccessStatusCode);
            }

            // Assertions
            using var scope = _factory.Services.CreateScope();

            using var context = scope.ServiceProvider.GetService<PaymentsContext>();
            var storedDeposits = await context.Deposits
                .Where(x => requests.Select(x => x.UserId).Contains(x.UserId))
                .ToListAsync();

            var investrClient = scope.ServiceProvider.GetService<ISmartInvestClient>();
            var sentDeposits = investrClient.SubmittedDeposits
                .Where(x => requests.Select(x => x.UserId).Contains(x.UserId))
                .ToList();

            Assert.Equal(sentDeposits.Count, storedDeposits.Count);
        }

        private static async Task<T> TryUntilSuccessful<T>(
            Func<Task<T>> @delegate,
            Func<T, bool> successCondition = null)
        {
            bool isSuccess = false;
            while (!isSuccess)
            {
                try
                {
                    var result = await @delegate();
                    if (successCondition == default || successCondition(result))
                    {
                        return result;
                    }
                }
                catch
                {
                }
            }

            return default;
        }

        private static IEnumerable<MakeDeposit.Request> GenerateMakeDepositRequests(int count)
        {
            var depositRequests = new List<MakeDeposit.Request>();

            for (int i = 0; i < count; i++)
            {
                depositRequests.Add(GenerateMakeDepositRequest());
            }

            return depositRequests;
        }


        private static MakeDeposit.Request GenerateMakeDepositRequest()
        {
            return new MakeDeposit.Request
            {
                UserId = Guid.NewGuid(),
                Amount = new Random().Next(100, 1000)
            };
        }
    }
}