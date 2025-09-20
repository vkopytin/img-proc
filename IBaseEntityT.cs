namespace Repository
{
    public interface IBaseEntity<T> : IBaseEntity
    {
        new T Id { get; set; }
    }
}