using ErrorOr;
using MediatR;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Features.PreviewCommunication;

public sealed class PreviewCommunicationHandler(IReportGenerator generator)
    : IRequestHandler<PreviewCommunicationQuery, ErrorOr<GeneratedReport>>
{
    public Task<ErrorOr<GeneratedReport>> Handle(PreviewCommunicationQuery request, CancellationToken cancellationToken)
    {
        var options = new ReportDataOptions(Language: "eng", Content: request.Content);
        return generator.GenerateAsync(request.Rsc, request.OrisCode, options, cancellationToken);
    }
}
