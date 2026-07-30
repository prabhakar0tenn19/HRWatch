using HRWatch.Application.Common.Abstractions;
using HRWatch.Domain.Common;
using HRWatch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Infrastructure.Persistence.Repositories;

/// <summary>
/// GENERIC REPOSITORY IMPLEMENTATION
/// 
/// Provides base CRUD operations for all entities.
/// Specific repositories inherit from this and add their own queries.
/// 
/// WHY USE REPOSITORY PATTERN WITH EF CORE?
/// EF Core's DbContext is already a Unit of Work + Repository.
/// But the Repository Pattern here:
///   1. Abstracts EF Core from Application layer (you could swap DB providers)
///   2. Makes unit testing easier (mock IRepository instead of DbContext)
///   3. Centralizes query logic, preventing scattered raw LINQ across handlers
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet   = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync([id], cancellationToken);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.ToListAsync(cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await _dbSet.AddRangeAsync(entities, cancellationToken);

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
