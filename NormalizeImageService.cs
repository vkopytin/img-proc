using ImgProc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class NormalizeImageService
{
  AppSettings appSettings;
  ILogger<NormalizeImageService> logger;

  public NormalizeImageService(
    IOptions<AppSettings> appSettings,
    ILogger<NormalizeImageService> logger
  )
  {
    this.appSettings = appSettings.Value;
    this.logger = logger;
  }

  public async Task Process()
  {
    await Task.Delay(100);
    logger.LogInformation("Processing image...");
  }
}
