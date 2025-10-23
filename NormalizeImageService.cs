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
    var blocks = query.Where(b => b.SourceUrl is null && b.Origin is not null).Take(10).ToArray();

    foreach (var item in blocks)
    {
      var isValid = Uri.IsWellFormedUriString(item.Origin, UriKind.Absolute);
      if (isValid)
      {
        var uri = new Uri(item.Origin!);
        var fileName = Path.GetFileName(uri.LocalPath);
        logger.LogInformation($"Processing image: {fileName}");

        var (result, error) = await this.NormalizeImage(uri);
        if (result is null)
        {
          logger.LogError(error);
          item.MediaError = error;
        }
        else
        {
          item.SourceUrl = result;
          item.MediaError = null;
        }
      }
      else
      {
        item.MediaError = "invalid-origin";
      }

      context.Update(item);
    }

    await context.SaveChangesAsync();
  }

  private async Task<(string? result, string? error)> NormalizeImage(Uri url)
  {
    var (imageData, error) = await this.DownloadImage(url);
    if (imageData is null)
    {
      return (null, error);
    }

    var fileName = Path.GetFileName(url.LocalPath);
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

  private async Task<(byte[]?, string? error)> DownloadImage(Uri url)
  {
    using var result = await httpClient.GetAsync(url);
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
}
