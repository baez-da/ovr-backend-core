using ErrorOr;
using MediatR;

namespace OVR.Modules.Entries.Features.ChangeEntryStatus;

public sealed record ChangeEntryStatusCommand(
    string EntryId,
    string NewStatus) : IRequest<ErrorOr<ChangeEntryStatusResponse>>;

public sealed record ChangeEntryStatusResponse(
    string Id,
    string OldStatus,
    string NewStatus);
