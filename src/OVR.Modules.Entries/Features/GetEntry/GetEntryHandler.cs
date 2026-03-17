using ErrorOr;
using MediatR;
using OVR.Modules.Entries.Persistence;

namespace OVR.Modules.Entries.Features.GetEntry;

public sealed class GetEntryHandler(IEntryRepository repository)
    : IRequestHandler<GetEntryQuery, ErrorOr<EntryResponse>>
{
    public async Task<ErrorOr<EntryResponse>> Handle(
        GetEntryQuery request,
        CancellationToken cancellationToken)
    {
        var entry = await repository.GetByIdAsync(request.EntryId, cancellationToken);
        if (entry is null)
            return Errors.EntryErrors.NotFound(request.EntryId);

        return new EntryResponse(
            entry.Id,
            entry.ParticipantId.Value,
            entry.EventRsc.Value,
            entry.CompetitorType.ToString(),
            entry.Organisation.Code,
            entry.Status.ToString(),
            entry.InscriptionStatus.ToString(),
            entry.RegisteredEventRsc?.Value,
            entry.Category,
            entry.TeamId?.Value,
            entry.Seed,
            entry.CreatedAt,
            entry.UpdatedAt);
    }
}
