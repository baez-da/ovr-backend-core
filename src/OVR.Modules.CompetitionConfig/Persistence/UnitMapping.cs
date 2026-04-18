using OVR.Modules.CompetitionConfig.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Persistence;

internal static class UnitMapping
{
    public static UnitDocument ToDocument(Unit unit) => new()
    {
        Id = unit.Id,
        EventRsc = unit.EventRsc.Value,
        PhaseCode = unit.PhaseCode,
        UnitNumber = unit.UnitNumber,
        CreatedAt = unit.CreatedAt
    };

    public static Unit ToDomain(UnitDocument doc)
    {
        var rsc = Rsc.Create(doc.Id);
        return Unit.Create(rsc);
    }
}
