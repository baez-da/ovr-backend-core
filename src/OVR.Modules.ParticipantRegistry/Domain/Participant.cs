using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Domain;

public sealed class Participant : AggregateRoot<string>
{
    public ParticipantId ParticipantId { get; private set; } = null!;
    public ParticipantType Type { get; private set; }
    public Description Description { get; private set; } = null!;
    public ExtendedDescription ExtendedDescription { get; private set; } = new();
    public string PrintName { get; private set; } = string.Empty;
    public string PrintInitialName { get; private set; } = string.Empty;
    public string TvName { get; private set; } = string.Empty;
    public string TvInitialName { get; private set; } = string.Empty;
    public string TvFamilyName { get; private set; } = string.Empty;
    public string PscbName { get; private set; } = string.Empty;
    public string PscbShortName { get; private set; } = string.Empty;
    public string PscbLongName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Participant() { }

    public static Participant Create(
        ParticipantType type,
        Description description,
        ExtendedDescription? extendedDescription,
        string printName,
        string printInitialName,
        string tvName,
        string tvInitialName,
        string tvFamilyName,
        string pscbName,
        string pscbShortName,
        string pscbLongName)
    {
        var participantId = ParticipantId.Generate();
        var participant = new Participant
        {
            Id = participantId.Value,
            ParticipantId = participantId,
            Type = type,
            Description = description,
            ExtendedDescription = extendedDescription ?? new ExtendedDescription(),
            PrintName = printName,
            PrintInitialName = printInitialName,
            TvName = tvName,
            TvInitialName = tvInitialName,
            TvFamilyName = tvFamilyName,
            PscbName = pscbName,
            PscbShortName = pscbShortName,
            PscbLongName = pscbLongName,
            CreatedAt = DateTime.UtcNow
        };

        participant.RaiseDomainEvent(new ParticipantCreatedEvent(
            participantId.Value,
            type.ToString(),
            description.Organisation.Code));

        return participant;
    }

    public static Participant Hydrate(
        ParticipantId participantId,
        ParticipantType type,
        Description description,
        ExtendedDescription? extendedDescription,
        string printName,
        string printInitialName,
        string tvName,
        string tvInitialName,
        string tvFamilyName,
        string pscbName,
        string pscbShortName,
        string pscbLongName,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        return new Participant
        {
            Id = participantId.Value,
            ParticipantId = participantId,
            Type = type,
            Description = description,
            ExtendedDescription = extendedDescription ?? new ExtendedDescription(),
            PrintName = printName,
            PrintInitialName = printInitialName,
            TvName = tvName,
            TvInitialName = tvInitialName,
            TvFamilyName = tvFamilyName,
            PscbName = pscbName,
            PscbShortName = pscbShortName,
            PscbLongName = pscbLongName,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
