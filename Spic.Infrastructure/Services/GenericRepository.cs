using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Interfaces;
using System.Linq.Expressions;
using System.Reflection;

namespace Spic.Infrastructure.Services
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;
        private static readonly PropertyInfo? _isActiveProperty = typeof(T).GetProperty("IsActive");
        private static readonly PropertyInfo? _updatedAtProperty = typeof(T).GetProperty("UpdatedAt");

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        /// <summary>
        /// Returns only active items if the entity has an IsActive property.
        /// If the entity has no IsActive property, returns all items.
        /// </summary>
        public IQueryable<T> GetAll()
        {
            if (_isActiveProperty != null)
            {
                // Build: x => x.IsActive == true
                var param = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(param, _isActiveProperty);
                var trueValue = Expression.Constant(true);
                var condition = Expression.Equal(property, trueValue);
                var lambda = Expression.Lambda<Func<T, bool>>(condition, param);

                return _dbSet.AsNoTracking().Where(lambda);
            }

            return _dbSet.AsNoTracking();
        }

        /// <summary>
        /// Returns ALL items including inactive — no filtering applied.
        /// </summary>
        public IQueryable<T> GetAllWithInactive()
        {
            return _dbSet.AsNoTracking();
        }

        /// <summary>
        /// Filtered queryable — builds WHERE clause in SQL, not in memory.
        /// </summary>
        public IQueryable<T> GetWhere(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.AsNoTracking().Where(predicate);
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<T> CreateAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// Finds existing entity by id, auto-maps ALL scalar properties from source.
        /// Preserves Id and CreatedAt; auto-sets UpdatedAt.
        /// No manual property mapping needed.
        /// </summary>
        public async Task<T?> PatchAsync(int id, T source)
        {
            var existing = await _dbSet.FindAsync(id);
            if (existing == null) return default;

            // Auto-map all scalar properties from source → existing
            _context.Entry(existing).CurrentValues.SetValues(source);

            // Preserve Id (prevent overwrite)
            _context.Entry(existing).Property("Id").IsModified = false;

            // Preserve CreatedAt — never overwrite on update
            if (_context.Entry(existing).Properties.Any(p => p.Metadata.Name == "CreatedAt"))
                _context.Entry(existing).Property("CreatedAt").IsModified = false;

            // Always set UpdatedAt to now
            if (_context.Entry(existing).Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                _context.Entry(existing).Property("UpdatedAt").CurrentValue = DateTime.Now;

            await _context.SaveChangesAsync();
            return existing;
        }

        /// <summary>
        /// Toggle IsActive status for entities that have the property.
        /// Returns null if entity not found or has no IsActive property.
        /// </summary>
        public async Task<T?> ChangeStatusAsync(int id, bool isActive)
        {
            if (_isActiveProperty == null)
                return default; // Entity doesn't have IsActive

            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return default;

            // Set IsActive
            _isActiveProperty.SetValue(entity, isActive);

            // Set UpdatedAt if the entity has it
            _updatedAtProperty?.SetValue(entity, DateTime.Now);

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
                return false;

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }
    }
}