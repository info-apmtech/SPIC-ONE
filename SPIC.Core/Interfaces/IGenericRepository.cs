using System.Linq.Expressions;

namespace SPIC.Core.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        // Returns only active items if entity has IsActive property, otherwise returns all
        IQueryable<T> GetAll();

        // Returns ALL items including inactive — no filtering
        IQueryable<T> GetAllWithInactive();

        // Filtered queryable — e.g., GetWhere(x => x.IsActive)
        IQueryable<T> GetWhere(Expression<Func<T, bool>> predicate);

        Task<T?> GetByIdAsync(int id);

        Task<T> CreateAsync(T entity);

        Task<T> UpdateAsync(T entity);

        // Finds existing by id, auto-maps all properties from source, sets UpdatedAt
        Task<T?> PatchAsync(int id, T source);

        Task<bool> DeleteAsync(int id);

        // Toggle IsActive status
        Task<T?> ChangeStatusAsync(int id, bool isActive);

        // Bulk check
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<T?> UpdatePropertyAsync(int id, string propertyName, object? value);
    }
}