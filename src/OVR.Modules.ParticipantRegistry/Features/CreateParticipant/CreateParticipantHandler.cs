using ErrorOr;
using MediatR;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Domain.NameSystem;
using OVR.Modules.ParticipantRegistry.Persistence;
using OVR.SharedKernel.Contracts;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Features.CreateParticipant;

public sealed class CreateParticipantHandler(
    IParticipantRepository repository,
    IPublisher publisher,
    ICommonCodeCache commonCodeCache,
    INameBuilder nameBuilder)
    : IRequestHandler<CreateParticipantCommand, ErrorOr<CreateParticipantResponse>>
{
    public async Task<ErrorOr<CreateParticipantResponse>> Handle(
        CreateParticipantCommand request,
        CancellationToken cancellationToken)
    {
        if (!commonCodeCache.Exists(CommonCodeTypes.Organisation, request.Organisation))
            return Errors.ParticipantErrors.InvalidOrganisation(request.Organisation);

        foreach (var fn in request.Functions)
        {
            if (!commonCodeCache.Exists(CommonCodeTypes.Discipline, fn.Discipline))
                return Errors.ParticipantErrors.InvalidDiscipline(fn.Discipline);

            if (!commonCodeCache.Exists(CommonCodeTypes.DisciplineFunction, fn.Function))
                return Errors.ParticipantErrors.InvalidFunction(fn.Function);
        }

        var gender = Gender.FromCode(request.Gender);
        var organisation = Organisation.Create(request.Organisation);
        var biographicData = BiographicData.Create(request.GivenName, request.FamilyName, gender, request.BirthDate, organisation);

        var functions = request.Functions
            .Select(f => ParticipantFunction.Create(f.Function, f.Discipline, f.IsMain))
            .ToList();

        var printName = nameBuilder.BuildPrintName(request.FamilyName, request.GivenName);
        var printInitialName = nameBuilder.BuildPrintInitialName(request.FamilyName, request.GivenName);
        var tvName = nameBuilder.BuildTvName(request.FamilyName, request.GivenName, request.Organisation);
        var tvInitialName = nameBuilder.BuildTvInitialName(request.FamilyName, request.GivenName, request.Organisation);
        var tvFamilyName = nameBuilder.BuildTvFamilyName(request.FamilyName);
        var pscbName = nameBuilder.BuildPscbName(request.FamilyName, request.GivenName);
        var pscbShortName = nameBuilder.BuildPscbShortName(request.FamilyName, request.GivenName);
        var pscbLongName = nameBuilder.BuildPscbLongName(request.FamilyName, request.GivenName);

        var participant = Participant.Create(
            biographicData, extendedDescription: null, functions,
            printName, printInitialName, tvName, tvInitialName,
            tvFamilyName, pscbName, pscbShortName, pscbLongName,
            request.PhotoUrl);

        await repository.AddAsync(participant, cancellationToken);

        foreach (var domainEvent in participant.DomainEvents)
            await publisher.Publish(domainEvent, cancellationToken);
        participant.ClearDomainEvents();

        return new CreateParticipantResponse(
            participant.Id,
            participant.PrintName, participant.PrintInitialName,
            participant.TvName, participant.TvInitialName, participant.TvFamilyName,
            participant.PscbName, participant.PscbShortName, participant.PscbLongName,
            participant.Functions.Select(f => new FunctionDto(f.Function, f.Discipline, f.IsMain)).ToList(),
            participant.CreatedAt,
            participant.PhotoUrl);
    }
}
