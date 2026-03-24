using MongoDB.Driver;
using OVR.Modules.Reporting.Persistence.Documents;

namespace OVR.Modules.Reporting.Persistence;

internal sealed class MongoReportPartialRepository(IMongoDatabase database) : IReportPartialRepository
{
    private IMongoCollection<ReportPartialDocument> Collection =>
        database.GetCollection<ReportPartialDocument>("reporting_partials");

    public async Task<IReadOnlyList<ReportPartialDocument>> GetAllAsync(CancellationToken ct)
    {
        return await Collection.Find(Builders<ReportPartialDocument>.Filter.Empty).ToListAsync(ct);
    }

    public async Task UpsertAsync(ReportPartialDocument doc, CancellationToken ct)
    {
        var filter = Builders<ReportPartialDocument>.Filter.Eq(d => d.Name, doc.Name);
        await Collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = true }, ct);
    }
}
