using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text.Json;
using ImageMagick;
using ImgProc;
using ImgProc.ImgBB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class NormalizeImageService
{
  HttpClient httpClient;
  MongoDbContext context;
  AppSettings appSettings;
  ILogger<NormalizeImageService> logger;

  public NormalizeImageService(
    HttpClient httpClient,
    MongoDbContext dbContext,
    IOptions<AppSettings> appSettings,
    ILogger<NormalizeImageService> logger
  )
  {
    this.httpClient = httpClient;
    context = dbContext;
    this.appSettings = appSettings.Value;
    this.logger = logger;
  }

  public async Task Process()
  {
    var query
    = from b in context.ArticleBlocks.AsEnumerable()
      orderby b.CreatedAt descending
      select b;
    var blocks = query.Where(b => b.Origin is null && b.SourceUrl is not null).Take(10).ToArray();

    foreach (var item in blocks)
    {
      var uri = new Uri(item.SourceUrl!);
      var fileName = Path.GetFileName(uri.LocalPath);
      logger.LogInformation($"Processing image: {fileName}");
      using var result = await httpClient.GetAsync(item.SourceUrl);
      if (result.IsSuccessStatusCode)
      {
        UInt16 orientationExif = 0;
        var imageData = await result.Content.ReadAsByteArrayAsync();
        try
        {
          var er = new ExifLib.ExifReader(new MemoryStream(imageData));
          er.GetTagValue(ExifLib.ExifTags.Orientation, out orientationExif);
        }
        catch (ExifLib.ExifLibException ex)
        {
        }

        var image = Bitmap.FromStream(new MemoryStream(imageData));
        if (orientationExif == 8)
        {
          image.RotateFlip(RotateFlipType.Rotate270FlipNone);
        }
        else if (orientationExif == 3)
        {
          image.RotateFlip(RotateFlipType.Rotate180FlipNone);
        }
        else if (orientationExif == 6)
        {
          image.RotateFlip(RotateFlipType.Rotate90FlipNone);
        }
        int width = appSettings.NormalizedWidth;
        var ratio = (float)image.Width / image.Height;

        var destRect = new Rectangle(0, 0, width, (int)(width / ratio));
        var destImage = new Bitmap(width, (int)(width / ratio));

        destImage.SetResolution(
            image.HorizontalResolution == 0 ? 300f : image.HorizontalResolution,
            image.VerticalResolution == 0 ? 300f : image.VerticalResolution
        );

        using (var graphics = Graphics.FromImage(destImage))
        {
          graphics.CompositingMode = CompositingMode.SourceOver;
          graphics.CompositingQuality = CompositingQuality.HighQuality;
          graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
          graphics.SmoothingMode = SmoothingMode.HighQuality;
          graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

          using (var wrapMode = new ImageAttributes())
          {
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
          }
        }

        using var m = new MemoryStream();
        destImage.Save(m, ImageFormat.Png);
        m.Position = 0;

        using var dest = new MagickImage(m);
        dest.SetCompression(CompressionMethod.WebP);
        byte[] res = dest.ToByteArray(MagickFormat.WebP);

        using (var m2 = new MemoryStream(res))
        {
          var (imgBbResult, err) = await this.CreateImageBB(m2, "image/webp", fileName);

          if (imgBbResult is null)
          {
            logger.LogError(err);
            continue;
          }

          item.Origin = item.SourceUrl;
          item.SourceUrl = imgBbResult.Data.Url;

          context.Update(item);
        }

        await context.SaveChangesAsync();
      }
    }
  }

  private async Task<(ImgBBUploadResult?, string? error)> CreateImageBB(Stream stream, string contentType, string fileName)
  {
    using var httpClient = new HttpClient();
    using MultipartFormDataContent multipartFormData = [];
    using var base64Stream = new CryptoStream(stream, new ToBase64Transform(), CryptoStreamMode.Read);

    httpClient.BaseAddress = new Uri(appSettings.ImgBb.BaseAddress);
    multipartFormData.Add(new StreamContent(base64Stream), "image");
    multipartFormData.Add(new StringContent(appSettings.ImgBb.Key), "key");
    multipartFormData.Add(new StringContent("description"), contentType);
    HttpResponseMessage httpResult = await httpClient.PostAsync($"/1/upload?name=image", multipartFormData);

    if (httpResult.IsSuccessStatusCode)
    {
      var retValue = await httpResult.Content.ReadAsStringAsync();
      var reyValue = JsonSerializer.Deserialize<ImgBBUploadResult>(retValue);

      return (reyValue, null);
    }

    var res = await httpResult.Content.ReadAsStringAsync();

    return (null, res);
  }
}
