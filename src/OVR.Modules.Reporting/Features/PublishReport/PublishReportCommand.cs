using ErrorOr;
using MediatR;

namespace OVR.Modules.Reporting.Features.PublishReport;

public record PublishReportCommand(string Rsc, string OrisCode, bool ForceGenerate = false)
    : IRequest<ErrorOr<PublishReportResponse>>;

public record PublishReportResponse(string Url, int Version, bool IsNew);
