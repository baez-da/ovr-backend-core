using MongoDB.Driver;
using OVR.Modules.Reporting.Persistence.Documents;

namespace OVR.Modules.Reporting.Persistence;

internal sealed class MongoReportTemplateRepository(IMongoDatabase database) : IReportTemplateRepository
{
    private IMongoCollection<ReportTemplateDocument> Collection =>
        database.GetCollection<ReportTemplateDocument>("reporting_templates");

    public async Task<ReportTemplateDocument?> FindAsync(string orisCode, string? discipline, CancellationToken ct)
    {
        var filter = Builders<ReportTemplateDocument>.Filter.Eq(d => d.OrisCode, orisCode);
        if (discipline is not null)
            filter &= Builders<ReportTemplateDocument>.Filter.Eq(d => d.Discipline, discipline);

        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<ReportTemplateDocument>> GetAllAsync(CancellationToken ct)
    {
        return await Collection.Find(Builders<ReportTemplateDocument>.Filter.Empty).ToListAsync(ct);
    }

    public async Task UpsertAsync(ReportTemplateDocument doc, CancellationToken ct)
    {
        var filter = Builders<ReportTemplateDocument>.Filter.Eq(d => d.OrisCode, doc.OrisCode)
            & Builders<ReportTemplateDocument>.Filter.Eq(d => d.Discipline, doc.Discipline)
            & Builders<ReportTemplateDocument>.Filter.Eq(d => d.ChampionshipCode, doc.ChampionshipCode);
        var existing = await Collection.Find(filter).Project(d => d.Id).FirstOrDefaultAsync(ct);
        if (existing is not null)
            doc.Id = existing;
        await Collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = true }, ct);
    }
}
