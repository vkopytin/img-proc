using ImgProc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class NormalizeImageService
{
  MongoDbContext context;
  AppSettings appSettings;
  ILogger<NormalizeImageService> logger;

  public NormalizeImageService(
    MongoDbContext dbContext,
    IOptions<AppSettings> appSettings,
    ILogger<NormalizeImageService> logger
  )
  {
    context = dbContext;
    this.appSettings = appSettings.Value;
    this.logger = logger;
  }

  public Task Process()
  {
    var query
    = from b in context.ArticleBlocks.AsEnumerable()
      orderby b.CreatedAt descending
      select b;
    var blocks = query.Where(b => b.Origin is null).Take(10).ToArray();

    foreach (var item in blocks)
    {
      logger.LogInformation($"Processing image: {item.SourceUrl}");
    }

    return Task.CompletedTask;
  }
}
