using System.ComponentModel.DataAnnotations.Schema;

namespace ImgProc.Db;

[Table("ArticleBlock")]
public class ArticleBlockRecord : BaseEntity<int>
{
  public string? Title { get; set; }
  public string? Description { get; set; }

  public string? Origin { get; set; }

  public DateTime UpdatedAt { get; set; }

  #region Navigation Properties
  public Guid? ArticleId { get; set; }

  #endregion

  #region Media Info
  public int? MediaId { get; set; }
  [ForeignKey("MediaId")]
  public ArticleBlockRecord? Media { get; set; }
  public string? MediaError { get; set; }
  public int? Width { get; set; }
  public int? Height { get; set; }
  public string? SourceUrl { get; set; }
  public string? FileName { get; set; }
  #endregion

  public string? Rank { get; set; }
}