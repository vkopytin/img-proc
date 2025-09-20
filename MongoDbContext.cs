using ImgProc.Db;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace ImgProc;

public class MongoDbContext : DbContext
{
  public DbSet<ArticleBlockRecord> ArticleBlocks { get; set; }

  public MongoDbContext(MongoClient client)
   : base(new DbContextOptionsBuilder<MongoDbContext>().UseMongoDB(client, "main").Options)
  {

  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<ArticleBlockRecord>().ToCollection("articleBlocks");
  }
}
