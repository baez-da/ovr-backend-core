using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Entries.Domain;

public sealed class Entry : AggregateRoot<string>
{
    public ParticipantId ParticipantId { get; private set; } = null!;
    public Rsc EventRsc { get; private set; } = null!;
    public CompetitorType CompetitorType { get; private set; }
    public Organisation Organisation { get; private set; } = null!;
    public EntryStatus Status { get; private set; }
    public InscriptionStatus InscriptionStatus { get; private set; }
    public Rsc? RegisteredEventRsc { get; private set; }
    public string? Category { get; private set; }
    public TeamId? TeamId { get; private set; }
    public string? Seed { get; private set; }
    public Dictionary<string, string>? EventEntries { get; private set; }
    public ExternalSystemId? ExternalId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Entry() { }

    public static Entry Create(
        ParticipantId participantId,
        Rsc eventRsc,
        CompetitorType competitorType,
        Organisation organisation,
        string? category = null,
        TeamId? teamId = null)
    {
        var entry = new Entry
        {
            Id = $"{participantId.Value}_{eventRsc.Value}",
            ParticipantId = participantId,
            EventRsc = eventRsc,
            CompetitorType = competitorType,
            Organisation = organisation,
            Status = EntryStatus.Entered,
            InscriptionStatus = InscriptionStatus.Pending,
            Category = category,
            TeamId = teamId,
            CreatedAt = DateTime.UtcNow
        };

        entry.RaiseDomainEvent(new EntryCreatedEvent(
            entry.Id,
            participantId.Value,
            eventRsc.Value,
            competitorType.ToString(),
            organisation.Code));

        return entry;
    }

    public static Entry CreateFromOdf(
        ParticipantId participantId,
        Rsc eventRsc,
        CompetitorType competitorType,
        Organisation organisation,
        Dictionary<string, string>? eventEntries,
        string? seed)
    {
        return new Entry
        {
            Id = $"{participantId.Value}_{eventRsc.Value}",
            ParticipantId = participantId,
            EventRsc = eventRsc,
            CompetitorType = competitorType,
            Organisation = organisation,
            Status = EntryStatus.Entered,
            InscriptionStatus = InscriptionStatus.Pending,
            EventEntries = eventEntries,
            Seed = seed,
            CreatedAt = DateTime.UtcNow
        };
        // No domain events — bulk import uses DtParticImportedEvent
    }

    public static Entry Hydrate(
        string id,
        ParticipantId participantId,
        Rsc eventRsc,
        CompetitorType competitorType,
        Organisation organisation,
        EntryStatus status,
        InscriptionStatus inscriptionStatus,
        Rsc? registeredEventRsc,
        string? category,
        TeamId? teamId,
        string? seed,
        Dictionary<string, string>? eventEntries,
        ExternalSystemId? externalId,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        return new Entry
        {
            Id = id,
            ParticipantId = participantId,
            EventRsc = eventRsc,
            CompetitorType = competitorType,
            Organisation = organisation,
            Status = status,
            InscriptionStatus = inscriptionStatus,
            RegisteredEventRsc = registeredEventRsc,
            Category = category,
            TeamId = teamId,
            Seed = seed,
            EventEntries = eventEntries,
            ExternalId = externalId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void ChangeStatus(EntryStatus newStatus)
    {
        var valid = (Status, newStatus) switch
        {
            (EntryStatus.Entered, EntryStatus.Active) => true,
            (EntryStatus.Entered, EntryStatus.Withdrawn) => true,
            (EntryStatus.Active, EntryStatus.Withdrawn) => true,
            (EntryStatus.Active, EntryStatus.DidNotStart) => true,
            (EntryStatus.Active, EntryStatus.Disqualified) => true,
            (EntryStatus.Active, EntryStatus.Suspended) => true,
            (EntryStatus.Active, EntryStatus.Replaced) => true,
            (EntryStatus.Suspended, EntryStatus.Active) => true,
            (EntryStatus.Suspended, EntryStatus.Withdrawn) => true,
            _ => false
        };

        if (!valid)
            throw new InvalidOperationException(
                $"Cannot transition entry status from {Status} to {newStatus}.");

        var oldStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new EntryStatusChangedEvent(
            Id, EventRsc.Value, oldStatus.ToString(), newStatus.ToString()));
    }

    public void ChangeInscriptionStatus(InscriptionStatus newStatus)
    {
        var valid = (InscriptionStatus, newStatus) switch
        {
            (InscriptionStatus.Pending, InscriptionStatus.Confirmed) => true,
            (InscriptionStatus.Pending, InscriptionStatus.Rejected) => true,
            (InscriptionStatus.Confirmed, InscriptionStatus.Cancelled) => true,
            _ => false
        };

        if (!valid)
            throw new InvalidOperationException(
                $"Cannot transition inscription status from {InscriptionStatus} to {newStatus}.");

        InscriptionStatus = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSeed(string? seed)
    {
        Seed = seed;
        UpdatedAt = DateTime.UtcNow;
    }
}
