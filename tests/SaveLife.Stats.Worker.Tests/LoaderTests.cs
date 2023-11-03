using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SaveLife.Stats.Worker.Tests
{
    [TestClass]
    public class LoaderTests
    {
        private readonly Loader? _loader;
        public LoaderTests()
        {
            _loader = TestWorkerFactory.BuildWorker();
        }

        [TestMethod]
        public async Task Loader_Should_Work()
        {
            _loader.Should().NotBeNull();
            await _loader!.StartAsync(CancellationToken.None);
            await _loader.ExecuteTask;
        }
    }
}
