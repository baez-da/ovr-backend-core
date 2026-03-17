using ErrorOr;
using MediatR;

namespace OVR.Modules.Entries.Features.WithdrawEntry;

public sealed record WithdrawEntryCommand(string EntryId) : IRequest<ErrorOr<WithdrawEntryResponse>>;

public sealed record WithdrawEntryResponse(string Id, string Status);
