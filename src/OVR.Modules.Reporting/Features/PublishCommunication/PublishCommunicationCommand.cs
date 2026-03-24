using ErrorOr;
using MediatR;
using OVR.Modules.Reporting.Features.PublishReport;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Features.PublishCommunication;

public record PublishCommunicationCommand(
    string Rsc, string OrisCode, CommunicationContent Content, bool ForceGenerate = false)
    : IRequest<ErrorOr<PublishReportResponse>>;
