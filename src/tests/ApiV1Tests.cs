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

            Assert.DoesNotThrow(() =>
                new ChekrV2Controller(logger)
                );
        }
    }
}