using MongoDB.Driver;
using OVR.Modules.Reporting.Persistence.Documents;

namespace OVR.Modules.Reporting.Persistence;

internal sealed class MongoReportTemplateRepository(IMongoDatabase database) : IReportTemplateRepository
{
    private IMongoCollection<ReportTemplateDocument> Collection =>
        database.GetCollection<ReportTemplateDocument>("reporting_templates");

    public async Task<ReportTemplateDocument?> FindAsync(string orisCode, string? disciplineCode, CancellationToken ct)
    {
        var filter = Builders<ReportTemplateDocument>.Filter.Eq(d => d.OrisCode, orisCode);
        if (disciplineCode is not null)
            filter &= Builders<ReportTemplateDocument>.Filter.Eq(d => d.DisciplineCode, disciplineCode);

        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<ReportTemplateDocument>> GetAllAsync(CancellationToken ct)
    {
        return await Collection.Find(Builders<ReportTemplateDocument>.Filter.Empty).ToListAsync(ct);
    }

    public async Task UpsertAsync(ReportTemplateDocument doc, CancellationToken ct)
    {
        var filter = Builders<ReportTemplateDocument>.Filter.Eq(d => d.OrisCode, doc.OrisCode)
            & Builders<ReportTemplateDocument>.Filter.Eq(d => d.DisciplineCode, doc.DisciplineCode)
            & Builders<ReportTemplateDocument>.Filter.Eq(d => d.ChampionshipCode, doc.ChampionshipCode);
        await Collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = true }, ct);
    }
}
