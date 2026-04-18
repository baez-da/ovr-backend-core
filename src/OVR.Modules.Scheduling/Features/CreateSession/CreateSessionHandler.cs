using ErrorOr;
using MediatR;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Errors;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Contracts;

namespace OVR.Modules.Scheduling.Features.CreateSession;

public sealed class CreateSessionHandler(
    ISessionRepository repository,
    ICommonCodeCache cache)
    : IRequestHandler<CreateSessionCommand, ErrorOr<CreateSessionResponse>>
{
    public async Task<ErrorOr<CreateSessionResponse>> Handle(
        CreateSessionCommand request,
        CancellationToken ct)
    {
        if (!cache.Exists(CommonCodeTypes.Venue, request.VenueCode))
            return SchedulingErrors.InvalidVenue(request.VenueCode);

        var existing = await repository.GetByCodeAsync(request.Code, ct);
        if (existing is not null)
            return SchedulingErrors.SessionAlreadyExists(request.Code);

        var session = Session.Create(
            request.Code, request.VenueCode, request.Name,
            request.StartDate, request.EndDate, request.Leadin);

        await repository.AddAsync(session, ct);

        return new CreateSessionResponse(
            session.Code, session.VenueCode, session.Name,
            session.StartDate, session.EndDate, session.Leadin, session.CreatedAt);
    }
}
