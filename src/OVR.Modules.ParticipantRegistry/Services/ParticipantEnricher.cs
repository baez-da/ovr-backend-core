using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Features.GetParticipant;
using OVR.SharedKernel.Contracts;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Services;

public sealed class ParticipantEnricher(ICommonCodeReader commonCodeReader)
{
    public async Task<LocalizedCode> EnrichCodeAsync(
        string type, string code, string language, CancellationToken ct)
    {
        var name = await commonCodeReader.GetNameAsync(type, code, ct);
        return new LocalizedCode(code, ToDto(name, language, code));
    }

    public async Task<List<FunctionResponse>> EnrichFunctionsAsync(
        IReadOnlyList<ParticipantFunction> functions, string language, CancellationToken ct)
    {
        var result = new List<FunctionResponse>(functions.Count);
        foreach (var fn in functions)
        {
            var fnName = await commonCodeReader.GetNameAsync(CommonCodeTypes.DisciplineFunction, fn.FunctionId, ct);
            var discName = await commonCodeReader.GetNameAsync(CommonCodeTypes.Discipline, fn.DisciplineCode, ct);
            result.Add(new FunctionResponse(
                new LocalizedCode(fn.FunctionId, ToDto(fnName, language, fn.FunctionId)),
                new LocalizedCode(fn.DisciplineCode, ToDto(discName, language, fn.DisciplineCode)),
                fn.IsMain));
        }
        return result;
    }

    private static LocalizedTextDto ToDto(MultilingualText? text, string language, string fallbackCode)
    {
        if (text is null) return new LocalizedTextDto(fallbackCode);
        var resolved = text.Resolve(language);
        return new LocalizedTextDto(resolved.Long, text.GetOrDefault(language)?.Short);
    }
}
