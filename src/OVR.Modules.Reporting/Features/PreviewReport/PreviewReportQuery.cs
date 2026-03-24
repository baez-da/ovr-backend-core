using ErrorOr;
using MediatR;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Features.PreviewReport;

public record PreviewReportQuery(string Rsc, string OrisCode) : IRequest<ErrorOr<GeneratedReport>>;
