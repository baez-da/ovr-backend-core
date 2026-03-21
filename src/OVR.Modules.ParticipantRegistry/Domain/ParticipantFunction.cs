using OVR.SharedKernel.Domain.Primitives;

namespace OVR.Modules.ParticipantRegistry.Domain;

public sealed class ParticipantFunction : ValueObject
{
    public string FunctionId { get; }
    public string DisciplineCode { get; }
    public bool IsMain { get; }

    private ParticipantFunction(string functionId, string disciplineCode, bool isMain)
    {
        FunctionId = functionId;
        DisciplineCode = disciplineCode;
        IsMain = isMain;
    }

    public static ParticipantFunction Create(string functionId, string disciplineCode, bool isMain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(disciplineCode);
        return new ParticipantFunction(functionId.Trim(), disciplineCode.Trim(), isMain);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FunctionId;
        yield return DisciplineCode;
        // IsMain is NOT part of identity — two functions with same FunctionId+DisciplineCode are equal regardless of IsMain
    }
}
