using ErrorOr;
using MediatR;

namespace OVR.Modules.Reporting.Features.GetReport;

public record GetReportQuery(string Rsc, string OrisCode) : IRequest<ErrorOr<GetReportResponse>>;

public record GetReportResponse(string Url, int Version, DateTime GeneratedAt);
