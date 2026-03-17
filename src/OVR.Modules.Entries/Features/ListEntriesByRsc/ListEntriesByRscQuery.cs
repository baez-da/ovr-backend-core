using ErrorOr;
using MediatR;
using OVR.Modules.Entries.Features.GetEntry;

namespace OVR.Modules.Entries.Features.ListEntriesByRsc;

public sealed record ListEntriesByRscQuery(string RscPrefix) : IRequest<ErrorOr<IReadOnlyList<EntryResponse>>>;
