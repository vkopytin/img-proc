using System;
using System.ComponentModel.DataAnnotations;
using ImgProc.Dto;
using Repository;

namespace ImgProc.Db;

public class ArticleBlockRecord : BaseEntity<int>
{

  public string Title { get; set; }
  public string Description { get; set; }

  public string Origin { get; set; }

  public DateTime UpdatedAt { get; set; }

  #region Navigation Properties
  public Guid ArticleId { get; set; }

  #endregion

  #region Media Info
  public int MediaId { get; set; }
  public ArticleBlockRecord Media { get; set; }
  public int? Width { get; set; }
  public int? Height { get; set; }
  public string SourceUrl { get; set; }
  public string FileName { get; set; }
  #endregion

  public string Rank { get; set; }

  public ArticleBlockModel ToDtObject()
  {
    var model = new ArticleBlockModel
    {
      Id = this.Id,
      Title = this.Title,
      Description = this.Description,
      CreatedAt = this.CreatedAt,
      UpdatedAt = this.UpdatedAt,
      ArticleId = this.ArticleId,
      MediaId = this.MediaId,
      Media = this.Media?.ToDtObject(),
      Width = this.Width ?? 0,
      Height = this.Height ?? 0,
      SourceUrl = this.SourceUrl,
      Origin = this.Origin,
      Rank = this.Rank,
      FileName = this.FileName,
    };

    return model;
  }
}
