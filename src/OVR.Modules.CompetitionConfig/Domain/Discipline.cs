using OVR.SharedKernel.Domain.Primitives;

namespace OVR.Modules.CompetitionConfig.Domain;

public sealed class Discipline : AggregateRoot<string>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private Discipline() { }

    public static Discipline Create(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Discipline { Id = code, Code = code, Name = name };
    }
}
