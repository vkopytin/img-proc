using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ImgProc;

public static class MongoExtensions
{
  public static MongoClient CreateMongoClient(this IConfiguration configuration, string mongoDBConnection)
  {
    var connectionString = configuration.GetConnectionString(mongoDBConnection);

    var settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));

    return new MongoClient(settings);
  }

  public static Dictionary<string, object> ToDictionary<T>(this T value)
  {
    var entityType = typeof(T);
    var dict = entityType.GetProperties()
        .Where(p => p.Name != "Id")
        .Select(p => new KeyValuePair<string, object>(p.Name, p.GetValue(value, null)));

    return dict.ToDictionary(d => d.Key, d => d.Value);
  }

  public static BsonDocument ToUpdateSet<T>(this T value)
  {
    var update = new BsonDocument("$set", new BsonDocument(value.ToDictionary()));
    return update;
  }

  public static async Task<long> RemoveRange<T>(this IMongoCollection<T> collection, IEnumerable<T> itemsToDelete, Func<T, object> getId)
  {
    var ids = itemsToDelete.Select(i => getId(i)).ToArray();
    var filter = Builders<T>.Filter.In(d => getId(d), ids);

    var result = await collection.DeleteManyAsync(filter);

    return result.DeletedCount;
  }
}

public static class StringExtensions
{
  public static string SortsBetween(this string prev, string next)
  {
    var pos = 0;
    var p = 0;
    var n = 0;
    var str = "";
    for (pos = 0; p == n; pos++) // find leftmost non-matching character
    {
      p = pos < prev.Length ? prev[pos] : 96;
      n = pos < next.Length ? next[pos] : 123;
    }
    str = prev.Substring(0, pos - 1); // copy identical part of string
    if (p == 96) // prev string equals beginning of next
    {
      while (n == 97) // next character is 'a'
      {
        n = pos < next.Length ? next[pos++] : 123;  // get char from next
        str += 'a'; // insert an 'a' to match the 'a'
      }
      if (n == 98) // next character is 'b'
      {
        str += 'a'; // insert an 'a' to match the 'b'
        n = 123; // set to end of alphabet
      }
    }
    else if (p + 1 == n) // found consecutive characters
    {
      str += Convert.ToChar(p); // insert character from prev
      n = 123; // set to end of alphabet
      while ((p = pos < prev.Length ? prev[pos++] : 96) == 122) // p='z'
      {
        str += 'z'; // insert 'z' to match 'z'
      }
    }
    return str + Convert.ToChar((int)Math.Ceiling((double)(p + n) / 2)); // append middle character
  }
}
