using System.Linq.Expressions;

namespace OVR.SharedKernel.Abstractions;

public interface IMongoRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(string id, T entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
