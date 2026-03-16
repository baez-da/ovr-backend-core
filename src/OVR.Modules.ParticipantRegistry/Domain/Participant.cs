using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;
using OVR.Modules.ParticipantRegistry.Domain.NameSystem;

namespace OVR.Modules.ParticipantRegistry.Domain;

public sealed class Participant : AggregateRoot<string>
{
    public ParticipantId ParticipantId { get; private set; } = null!;
    public ParticipantType Type { get; private set; }
    public Description Description { get; private set; } = null!;
    public ExtendedDescription ExtendedDescription { get; private set; } = new();
    public string PrintName { get; private set; } = string.Empty;
    public string TvName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Participant() { } // For MongoDB deserialization

    public static Participant Create(
        ParticipantId participantId,
        ParticipantType type,
        Description description)
    {
        var participant = new Participant
        {
            Id = participantId.Value,
            ParticipantId = participantId,
            Type = type,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        participant.PrintName = NameRules.BuildPrintName(description.FamilyName, description.GivenName);
        participant.TvName = NameRules.BuildTvName(description.FamilyName, description.GivenName);

        participant.RaiseDomainEvent(new ParticipantCreatedEvent(
            participantId.Value,
            type.ToString(),
            description.Organisation.Code));

        return participant;
    }

    public void UpdateDescription(Description newDescription)
    {
        Description = newDescription;
        PrintName = NameRules.BuildPrintName(newDescription.FamilyName, newDescription.GivenName);
        TvName = NameRules.BuildTvName(newDescription.FamilyName, newDescription.GivenName);
        UpdatedAt = DateTime.UtcNow;
    }
}
