using System;
using System.Text.Json.Serialization;
using ImgProc;

namespace ImgProc.Dto
{
  using System.ComponentModel.DataAnnotations;
  using Converters;
  using Db;

  public class ArticleBlockModel
  {
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("articleId")]
    public Guid? ArticleId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
    //[DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
    [JsonPropertyName("createdAt")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime UpdatedAt { get; set; }

    #region Media Info
    [JsonPropertyName("mediaId")]
    public int? MediaId { get; set; }
    //[JsonPropertyName("media")]
    public ArticleBlockModel Media { get; set; }
    [JsonPropertyName("width")]
    public int Width { get; set; }
    [JsonPropertyName("height")]
    public int Height { get; set; }
    [JsonPropertyName("sourceUrl")]
    public string SourceUrl { get; set; }
    [JsonPropertyName("origin")]
    public string Origin { get; set; }
    [JsonPropertyName("fileName")]
    public string FileName { get; set; }
    [JsonPropertyName("image")]
    public string Image
    {
      get
      {
        if (string.IsNullOrEmpty(this.Origin))
        {
          return null;
        }
        return $"https://vkopytin.blob.core.windows.net/normalized/{this.Id}";
      }
    }
    #endregion

    public string Rank { get; set; }

    public ArticleBlockRecord ToDbObject()
    {
      return new ArticleBlockRecord
      {
        Id = this.Id,
        Title = this.Title,
        Description = this.Description,
        CreatedAt = this.CreatedAt,
        UpdatedAt = this.UpdatedAt,
        ArticleId = this.ArticleId ?? Guid.Empty,
        MediaId = this.MediaId ?? 0,
        Media = this.Media?.ToDbObject(),
        Width = this.Width,
        Height = this.Height,
        SourceUrl = this.SourceUrl,
        Origin = this.Origin,
        Rank = this.Rank,
        FileName = this.FileName,
      };
    }
  }
}