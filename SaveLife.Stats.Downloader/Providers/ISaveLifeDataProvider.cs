using SaveLife.Stats.Downloader.Models;

namespace SaveLife.Stats.Downloader.Providers
{
    public interface ISaveLifeDataProvider
    {
        Task<SLOriginResponse> LoadDataAsync(SLOriginDataRequest request, CancellationToken cancellationToken);
    }
}
