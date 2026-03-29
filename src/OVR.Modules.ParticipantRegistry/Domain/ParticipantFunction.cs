using OVR.SharedKernel.Domain.Primitives;

namespace OVR.Modules.ParticipantRegistry.Domain;

public sealed class ParticipantFunction : ValueObject
{
    public string Function { get; }
    public string Discipline { get; }
    public bool IsMain { get; }
    public string? IFId { get; }

    private ParticipantFunction(string function, string discipline, bool isMain, string? ifId)
    {
        Function = function;
        Discipline = discipline;
        IsMain = isMain;
        IFId = ifId;
    }

    public static ParticipantFunction Create(
        string function, string discipline, bool isMain, string? ifId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(function);
        ArgumentException.ThrowIfNullOrWhiteSpace(discipline);
        return new ParticipantFunction(function.Trim(), discipline.Trim(), isMain, ifId?.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Function;
        yield return Discipline;
        // IsMain and IFId are NOT part of identity
    }
}
