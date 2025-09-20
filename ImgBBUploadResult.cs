
using System.Text.Json.Serialization;


namespace ImgProc.ImgBB;

public class ImageInfo
{
  [JsonPropertyName("filename")]
  public string? FileName { get; set; }
  [JsonPropertyName("name")]
  public string? Name { get; set; }
  [JsonPropertyName("mime")]
  public string? Mime { get; set; }
  [JsonPropertyName("extension")]
  public string? Extension { get; set; }
  [JsonPropertyName("url")]
  public string? Url { get; set; }
}

public class ImgBBImageInfo
{
  [JsonPropertyName("delete_url")]
  public string? DeleteUrl { get; set; }

  [JsonPropertyName("display_url")]
  public string? DisplayUrl { get; set; }
  [JsonPropertyName("id")]
  public string? Id { get; set; }
  [JsonPropertyName("title")]
  public string? Title { get; set; }
  [JsonPropertyName("url_viewer")]
  public string? UrlViewer { get; set; }
  [JsonPropertyName("url")]
  public string? Url { get; set; }
  [JsonPropertyName("size")]
  public int Size { get; set; }
  [JsonPropertyName("width")]
  public int Width { get; set; }
  [JsonPropertyName("height")]
  public int Height { get; set; }
  [JsonPropertyName("time")]
  public long Time { get; set; }
  [JsonPropertyName("expiration")]
  public long Expiration { get; set; }
  [JsonPropertyName("image")]
  public ImageInfo? Image { get; set; }
  [JsonPropertyName("thumb")]
  public ImageInfo? Thumb { get; set; }
}

public class ImgBBUploadResult
{
  [JsonPropertyName("data")]
  public ImgBBImageInfo? Data { get; set; }
  [JsonPropertyName("success")]
  public bool Success { get; set; }
  [JsonPropertyName("status")]
  public int Status { get; set; }
}
