using MongoDB.Driver;
using OVR.Modules.Reporting.Persistence.Documents;

namespace OVR.Modules.Reporting.Persistence;

internal sealed class MongoReportRecordRepository(IMongoDatabase database) : IReportRecordRepository
{
    private IMongoCollection<ReportRecordDocument> Collection =>
        database.GetCollection<ReportRecordDocument>("reporting_records");

    public async Task<ReportRecordDocument?> GetLatestAsync(string rsc, string orisCode, CancellationToken ct)
    {
        return await Collection
            .Find(d => d.Rsc == rsc && d.OrisCode == orisCode)
            .SortByDescending(d => d.Version)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(ReportRecordDocument doc, CancellationToken ct)
    {
        await Collection.InsertOneAsync(doc, cancellationToken: ct);
    }
}
