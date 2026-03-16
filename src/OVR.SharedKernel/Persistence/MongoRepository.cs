using System.Linq.Expressions;
using MongoDB.Driver;
using OVR.SharedKernel.Abstractions;

namespace OVR.SharedKernel.Persistence;

public class MongoRepository<T>(IMongoCollection<T> collection) : IMongoRepository<T> where T : class
{
    public async Task<T?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await collection.Find(Builders<T>.Filter.Eq("_id", id)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await collection.Find(predicate).ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await collection.InsertOneAsync(entity, cancellationToken: ct);

    public async Task UpdateAsync(string id, T entity, CancellationToken ct = default) =>
        await collection.ReplaceOneAsync(Builders<T>.Filter.Eq("_id", id), entity, cancellationToken: ct);

    public async Task DeleteAsync(string id, CancellationToken ct = default) =>
        await collection.DeleteOneAsync(Builders<T>.Filter.Eq("_id", id), ct);
}
