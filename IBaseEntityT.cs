namespace ImgProc.Db;

public interface IBaseEntity<T> : IBaseEntity
{
  new T Id { get; set; }
}
