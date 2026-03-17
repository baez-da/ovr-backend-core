using ErrorOr;
using MediatR;
using OVR.Modules.Entries.Features.GetEntry;
using OVR.Modules.Entries.Persistence;

namespace OVR.Modules.Entries.Features.ListEntriesByRsc;

public sealed class ListEntriesByRscHandler(IEntryRepository repository)
    : IRequestHandler<ListEntriesByRscQuery, ErrorOr<IReadOnlyList<EntryResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<EntryResponse>>> Handle(
        ListEntriesByRscQuery request,
        CancellationToken cancellationToken)
    {
        var entries = await repository.FindByRscPrefixAsync(request.RscPrefix, cancellationToken);

        var responses = entries.Select(e => new EntryResponse(
            e.Id,
            e.ParticipantId.Value,
            e.EventRsc.Value,
            e.CompetitorType.ToString(),
            e.Organisation.Code,
            e.Status.ToString(),
            e.InscriptionStatus.ToString(),
            e.RegisteredEventRsc?.Value,
            e.Category,
            e.TeamId?.Value,
            e.Seed,
            e.CreatedAt,
            e.UpdatedAt)).ToList();

        return responses;
    }
}
