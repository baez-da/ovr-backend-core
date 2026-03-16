using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Domain;

public sealed class Event : AggregateRoot<string>
{
    public Rsc Rsc { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public Gender Gender { get; private set; } = null!;

    private Event() { }

    public static Event Create(Rsc rsc, string name, Gender gender)
    {
        return new Event { Id = rsc.Value, Rsc = rsc, Name = name, Gender = gender };
    }
}
