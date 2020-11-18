using System;
using System.Threading.Tasks;
using api.Controllers.v1;
using api.Controllers.v2;
using Castle.Core.Logging;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace tests
{
    public class ApiV1Tests
    {
        [Test]
        public void CreateDefaultInstance()
        {
            var logger = A.Fake<ILogger<ChekrController>>();

            var controller = new ChekrController(logger);
        }
    }

    public class ApiV2Tests
    {
        [Test]
        public void CreateInstance()
        {
            var logger = A.Fake<ILogger<ChekrV2Controller>>();

            var dataAccess = A.Fake<IDataAccess>();
            var apiKeyRetriever = A.Fake<IApiKeyRetriever>();
            var rateLimit = A.Fake<IRateLimit>();
            var timeProvider = A.Fake<ITimeProvider>();

            var chekr = new DomainChekr(
                dataAccess
                , apiKeyRetriever
                , rateLimit
                , timeProvider
            );

            TestDelegate act = () => new ChekrV2Controller(logger, chekr);

            Assert.DoesNotThrow(act);
        }

        [Test]
        public async Task CallMethod()
        {
            var logger = A.Fake<ILogger<ChekrV2Controller>>();

            var dataAccess = A.Fake<IDataAccess>();
            var apiKeyRetriever = A.Fake<IApiKeyRetriever>();
            var rateLimit = A.Fake<IRateLimit>();
            var timeProvider = A.Fake<ITimeProvider>();

            var chekr = new DomainChekr(
                dataAccess
                , apiKeyRetriever
                , rateLimit
                , timeProvider
            );

            var controller = new ChekrV2Controller(logger, chekr);
            var res = await controller.GetAsync("asdf");
        }
    }

    public class DomainChekrTests
    {
        [Test]
        public void CreateInstance()
        {
            var dataAccess = A.Fake<IDataAccess>();
            var apiKeyRetriever = A.Fake<IApiKeyRetriever>();
            var rateLimit = A.Fake<IRateLimit>();
            var timeProvider = A.Fake<ITimeProvider>();

            var chekr = new DomainChekr(
                dataAccess
                , apiKeyRetriever
                , rateLimit
                , timeProvider
                );

            Assert.Pass();
        }
    }
}