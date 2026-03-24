using MongoDB.Driver;
using OVR.Modules.Reporting.Persistence.Documents;

namespace OVR.Modules.Reporting.Persistence;

internal sealed class MongoReportLayoutRepository(IMongoDatabase database) : IReportLayoutRepository
{
    private IMongoCollection<ReportLayoutDocument> Collection =>
        database.GetCollection<ReportLayoutDocument>("reporting_layouts");

    public async Task<ReportLayoutDocument?> FindAsync(string component, string? disciplineCode, CancellationToken ct)
    {
        var filter = Builders<ReportLayoutDocument>.Filter.Eq(d => d.Component, component);
        if (disciplineCode is not null)
            filter &= Builders<ReportLayoutDocument>.Filter.Eq(d => d.DisciplineCode, disciplineCode);

        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task UpsertAsync(ReportLayoutDocument doc, CancellationToken ct)
    {
        var filter = Builders<ReportLayoutDocument>.Filter.Eq(d => d.Component, doc.Component)
            & Builders<ReportLayoutDocument>.Filter.Eq(d => d.DisciplineCode, doc.DisciplineCode)
            & Builders<ReportLayoutDocument>.Filter.Eq(d => d.ChampionshipCode, doc.ChampionshipCode);
        await Collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = true }, ct);
    }
}
