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

public class NormalizeMediaService
{
  HttpClient httpClient;
  MongoDbContext context;
  AppSettings appSettings;
  ILogger<NormalizeMediaService> logger;

  public NormalizeMediaService(
    HttpClient httpClient,
    MongoDbContext dbContext,
    IOptions<AppSettings> appSettings,
    ILogger<NormalizeMediaService> logger
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
    var blocks = query.Where(b => b.SourceUrl is null && b.Origin is not null).Take(10).ToArray();

    foreach (var item in blocks)
    {
      var isValid = Uri.IsWellFormedUriString(item.Origin, UriKind.Absolute);
      if (!isValid)
      {
        item.MediaError = "invalid-origin";
        logger.LogError($"Invalid origin URL: {item.Origin}");
        context.Update(item);

        continue;
      }

      var uri = new Uri(item.Origin!);
      var fileName = Path.GetFileName(uri.LocalPath);
      logger.LogInformation($"Processing file: {fileName}");

      using var result = await httpClient.GetAsync(uri);
      var contentType = await ReadContentType(result, 6);
      logger.LogInformation($"Content type: {contentType}");
      if (contentType.StartsWith("image/"))
      {
        logger.LogInformation($"Processing image: {fileName}");

        var (imageData, error) = await this.ReadImage(result);
        if (imageData is null)
        {
          logger.LogError(error);
          item.MediaError = error;
          context.Update(item);

          continue;
        }

        var (normalizeResult, normalizeError) = await this.NormalizeImage(imageData, fileName);
        if (normalizeResult is null)
        {
          logger.LogError(normalizeError);
          item.MediaError = normalizeError;
          context.Update(item);

          continue;
        }

        item.Description = $"<img src=\"{normalizeResult}\" width=\"{item.Width}\" height=\"{item.Height}\" alt=\"{fileName}\" />";
        item.SourceUrl = normalizeResult;
        item.MediaError = null;

        context.Update(item);
      }
      else // toDO: Implement more normalizations logic here.
      {
        logger.LogError($"Invalid content type: {contentType}");
        item.MediaError = "invalid-content-type";
        context.Update(item);
      }
    }

    await context.SaveChangesAsync();
  }

  private async Task<(string? result, string? error)> NormalizeImage(byte[] imageData, string fileName)
  {
    UInt16 orientationExif = 0;
    try
    {
      try
      {
        var er = new ExifLib.ExifReader(new MemoryStream(imageData));
        er.GetTagValue(ExifLib.ExifTags.Orientation, out orientationExif);
      }
      catch (ExifLib.ExifLibException ex)
      {
      }

      var image = Image.FromStream(new MemoryStream(imageData));
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

      using var graphics = Graphics.FromImage(destImage);
      graphics.CompositingMode = CompositingMode.SourceOver;
      graphics.CompositingQuality = CompositingQuality.HighQuality;
      graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
      graphics.SmoothingMode = SmoothingMode.HighQuality;
      graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

      using var wrapMode = new ImageAttributes();

      wrapMode.SetWrapMode(WrapMode.TileFlipXY);
      graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);

      using var m = new MemoryStream();
      destImage.Save(m, ImageFormat.Png);
      m.Position = 0;

      using var dest = new MagickImage(m);
      dest.SetCompression(CompressionMethod.WebP);
      var res = dest.ToByteArray(MagickFormat.WebP);

      using var m2 = new MemoryStream(res);
      var (imgBbResult, err) = await this.CreateImageBB(m2, "image/webp", fileName);

      if (imgBbResult is null)
      {
        return (null, err);
      }

      return (imgBbResult.Data.Url, null);
    }
    catch (Exception ex)
    {
      return (null, ex.InnerException is null ? ex.ToString() : ex.InnerException.ToString());
    }
  }

  private async Task<(byte[]?, string? error)> ReadImage(HttpResponseMessage result)
  {
    if (result.IsSuccessStatusCode)
    {
      var imageData = await result.Content.ReadAsByteArrayAsync();

      return (imageData, null);
    }

    try
    {
      var customContent = await result.Content.ReadAsStringAsync();
      return (null, customContent);
    }
    catch (Exception ex)
    {
      return (null, ex.InnerException is null ? ex.Message : ex.InnerException.Message);
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

  static async Task<string> ReadContentType(HttpResponseMessage result, int length)
  {
    if (result.Content.Headers.ContentType?.MediaType is not null)
    {
      return result.Content.Headers.ContentType.MediaType;
    }
    var stream = await result.Content.ReadAsStreamAsync();
    using StreamReader reader = new StreamReader(stream);

    var magicLength = 6;
    var magicCode = new byte[magicLength];
    await stream.ReadAsync(magicCode, 0, magicLength);
    stream.Position = 0;

    var magicString = System.Text.Encoding.UTF8.GetString(magicCode);

    switch (magicString)
    {
      case "\x89PNG\r\n":
        return "image/png";
      case "\xFF\xD8\xFF":
        return "image/jpeg";
      case "GIF87a":
      case "GIF89a":
        return "image/gif";
      default:
        return "application/octet-stream";
    }
  }
}
