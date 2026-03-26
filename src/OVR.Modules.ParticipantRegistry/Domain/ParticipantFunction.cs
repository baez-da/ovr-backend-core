using OVR.SharedKernel.Domain.Primitives;

namespace OVR.Modules.ParticipantRegistry.Domain;

public sealed class ParticipantFunction : ValueObject
{
    public string Function { get; }
    public string Discipline { get; }
    public bool IsMain { get; }

    private ParticipantFunction(string function, string discipline, bool isMain)
    {
        Function = function;
        Discipline = discipline;
        IsMain = isMain;
    }

    public static ParticipantFunction Create(string function, string discipline, bool isMain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(function);
        ArgumentException.ThrowIfNullOrWhiteSpace(discipline);
        return new ParticipantFunction(function.Trim(), discipline.Trim(), isMain);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Function;
        yield return Discipline;
        // IsMain is NOT part of identity — two functions with same Function+Discipline are equal regardless of IsMain
    }
}
