using OVR.Modules.CompetitionConfig.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Persistence;

internal static class EventMapping
{
    public static EventDocument ToDocument(Event @event) => new()
    {
        Id = @event.Id,
        Discipline = @event.Discipline,
        Gender = @event.Gender.Value,
        EventCode = @event.EventCode,
        Modifier = @event.Modifier,
        Name = @event.Name,
        Format = @event.Format?.ToString(),
        Size = @event.Size,
        Phases = @event.Phases
            .Select(p => new PhaseSubDocument { Code = p.Code, Order = p.Order, UnitCount = p.UnitCount })
            .ToList(),
        CreatedAt = @event.CreatedAt,
        StructureGeneratedAt = @event.StructureGeneratedAt
    };

    public static Event ToDomain(EventDocument doc)
    {
        var rsc = Rsc.Create(doc.Id);
        var gender = Gender.FromCode(doc.Gender);
        var evt = Event.Create(rsc, doc.Discipline, gender, doc.EventCode, doc.Modifier, doc.Name);

        if (doc.Format is not null && doc.Size.HasValue && doc.StructureGeneratedAt.HasValue)
        {
            evt.HydrateFromStorage(
                Enum.Parse<CompetitionFormat>(doc.Format),
                doc.Size.Value,
                doc.Phases.Select(p => (p.Code, p.Order, p.UnitCount)).ToList(),
                doc.StructureGeneratedAt.Value);
        }

        return evt;
    }
}
