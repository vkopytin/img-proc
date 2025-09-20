using Microsoft.Extensions.Logging;

public class NormalizeImageService
{
  ILogger<NormalizeImageService> logger;

  public NormalizeImageService(ILogger<NormalizeImageService> logger)
  {
    this.logger = logger;
  }

  public async Task Process()
  {
    await Task.Delay(100);
    logger.LogInformation("Processing image...");
  }
}
