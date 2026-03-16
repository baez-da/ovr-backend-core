using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Entries.Domain;

public sealed class Entry : AggregateRoot<string>
{
    public string ParticipantId { get; private set; } = string.Empty;
    public Rsc EventRsc { get; private set; } = null!;
    public EntryStatus Status { get; private set; } = EntryStatus.Entered;
    public DateTime CreatedAt { get; private set; }

    private Entry() { }

    public static Entry Create(string participantId, Rsc eventRsc)
    {
        return new Entry
        {
            Id = $"{participantId}_{eventRsc.Value}",
            ParticipantId = participantId,
            EventRsc = eventRsc,
            Status = EntryStatus.Entered,
            CreatedAt = DateTime.UtcNow
        };
    }
}
