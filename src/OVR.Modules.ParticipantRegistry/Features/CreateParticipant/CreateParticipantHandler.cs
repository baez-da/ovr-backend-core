using ErrorOr;
using MediatR;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Domain.NameSystem;
using OVR.Modules.ParticipantRegistry.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Features.CreateParticipant;

public sealed class CreateParticipantHandler(
    IParticipantRepository repository,
    IPublisher publisher,
    ICommonCodeReader commonCodeReader,
    INameBuilder nameBuilder)
    : IRequestHandler<CreateParticipantCommand, ErrorOr<CreateParticipantResponse>>
{
    public async Task<ErrorOr<CreateParticipantResponse>> Handle(
        CreateParticipantCommand request,
        CancellationToken cancellationToken)
    {
        // Level 2: validate Organisation exists in CommonCodes
        var organisationExists = await commonCodeReader.ExistsAsync(CommonCodeTypes.Organisation, request.Organisation, cancellationToken);
        if (!organisationExists)
            return Errors.ParticipantErrors.InvalidOrganisation(request.Organisation);

        // Level 2: validate each function's discipline and function code
        foreach (var fn in request.Functions)
        {
            var discExists = await commonCodeReader.ExistsAsync(CommonCodeTypes.Discipline, fn.DisciplineCode, cancellationToken);
            if (!discExists)
                return Errors.ParticipantErrors.InvalidDiscipline(fn.DisciplineCode);

            var fnExists = await commonCodeReader.ExistsAsync(CommonCodeTypes.DisciplineFunction, fn.FunctionId, cancellationToken);
            if (!fnExists)
                return Errors.ParticipantErrors.InvalidFunction(fn.FunctionId);
        }

        // Build value objects
        var gender = Gender.FromCode(request.GenderCode);
        var organisation = Organisation.Create(request.Organisation);
        var description = Description.Create(request.GivenName, request.FamilyName, gender, request.BirthDate, organisation);

        var functions = request.Functions
            .Select(f => ParticipantFunction.Create(f.FunctionId, f.DisciplineCode, f.IsMain))
            .ToList();

        // Build names via domain service
        var printName = nameBuilder.BuildPrintName(request.FamilyName, request.GivenName);
        var printInitialName = nameBuilder.BuildPrintInitialName(request.FamilyName, request.GivenName);
        var tvName = nameBuilder.BuildTvName(request.FamilyName, request.GivenName, request.Organisation);
        var tvInitialName = nameBuilder.BuildTvInitialName(request.FamilyName, request.GivenName, request.Organisation);
        var tvFamilyName = nameBuilder.BuildTvFamilyName(request.FamilyName);
        var pscbName = nameBuilder.BuildPscbName(request.FamilyName, request.GivenName);
        var pscbShortName = nameBuilder.BuildPscbShortName(request.FamilyName, request.GivenName);
        var pscbLongName = nameBuilder.BuildPscbLongName(request.FamilyName, request.GivenName);

        // Create aggregate (generates ID internally)
        var participant = Participant.Create(
            description, extendedDescription: null, functions,
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
            participant.Functions.Select(f => new FunctionDto(f.FunctionId, f.DisciplineCode, f.IsMain)).ToList(),
            participant.CreatedAt,
            participant.PhotoUrl);
    }
}
