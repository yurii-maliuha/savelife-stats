using SaveLife.Stats.Worker.Models;

namespace SaveLife.Stats.Worker.Providers
{
    public interface ISaveLifeDataProvider
    {
        Task<SLOriginResponse> LoadDataAsync(SLOriginDataRequest request, CancellationToken cancellationToken);
    }
}
