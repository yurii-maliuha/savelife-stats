using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaveLife.Stats.Worker.Tests.Stubs;

namespace SaveLife.Stats.Worker.Tests
{
    [TestClass]
    public class LoaderTests
    {
        private PathResolverStub _pathResolver;

        public LoaderTests()
        {
            _pathResolver = new PathResolverStub();
        }

        [TestInitialize]
        public void Setup()
        {
            string transactionsPath = Path.Combine(_pathResolver.ResolveTransactionsPath(), $"transactions_1-2023.json");
            var historyPath = Path.Combine(_pathResolver.ResolveHistoryPath(), "history.json");
            if (File.Exists(transactionsPath))
            {
                File.Delete(transactionsPath);
            }

            if (File.Exists(historyPath))
            {
                File.Delete(historyPath);
            }
        }

        [TestMethod]
        public async Task Loader_Should_Load_All_Transactions()
        {
            var transactionsCount = 10;
            var loaderWorker = TestWorkerFactory.BuildWorker((IServiceProvider sp) => new SaveLifeDataProviderStub(sp, transactionsCount));

            loaderWorker.Should().NotBeNull();

            await loaderWorker!.StartAsync(CancellationToken.None);
            await loaderWorker.ExecuteTask;

            string filePath = Path.Combine(_pathResolver.ResolveTransactionsPath(), $"transactions_1-2023.json");
            File.Exists(filePath).Should().BeTrue();

            var lines = File.ReadAllLines(filePath);
            lines.Distinct().Count().Should().Be(transactionsCount);
        }

        [TestMethod]
        public async Task Loader_Should_Load_WithPaging_Transactions_With_Tiny_TimeStep()
        {
            var transactionsCount = 1000;
            var storedTransactionsCount = 20;
            var configuration = new Dictionary<string, string>()
            {
                { "DataSource:BatchSize", "5" },
                { "Loader:MaxIterationsCount", "4" },
                { "Loader:LoadFrom", "2023-01-01T00:00:00" },
                { "Loader:LoadTo", "2023-01-01T00:00:05" }
            };
            var loaderWorker = TestWorkerFactory.BuildWorker((IServiceProvider sp) => new SaveLifeDataProviderStub(sp, transactionsCount), configuration);

            loaderWorker.Should().NotBeNull();

            await loaderWorker!.StartAsync(CancellationToken.None);
            await loaderWorker.ExecuteTask;

            string filePath = Path.Combine(_pathResolver.ResolveTransactionsPath(), $"transactions_1-2023.json");
            File.Exists(filePath).Should().BeTrue();

            var lines = File.ReadAllLines(filePath);
            lines.Distinct().Count().Should().Be(storedTransactionsCount);
        }

        [TestMethod]
        public async Task Loader_Should_Load_WithPaging_Transactions_With_Identical_Date()
        {
            var transactionsCount = 40;
            var storedTransactionsCount = 35;
            var configuration = new Dictionary<string, string>()
            {
                { "DataSource:BatchSize", "5" },
                { "Loader:MaxIterationsCount", "7" },
                { "Loader:LoadFrom", "2023-01-01T00:00:00" },
                { "Loader:LoadTo", "2023-01-01T10:10:00" }
            };
            var loaderWorker = TestWorkerFactory.BuildWorker((IServiceProvider sp) => new SaveLifeDataWithIdenticalDatesProviderStub(sp, transactionsCount), configuration);

            loaderWorker.Should().NotBeNull();

            await loaderWorker!.StartAsync(CancellationToken.None);
            await loaderWorker.ExecuteTask;

            string filePath = Path.Combine(_pathResolver.ResolveTransactionsPath(), $"transactions_1-2023.json");
            File.Exists(filePath).Should().BeTrue();

            var lines = File.ReadAllLines(filePath);
            lines.Distinct().Count().Should().Be(storedTransactionsCount);
        }
    }
}
