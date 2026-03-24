using ErrorOr;
using MediatR;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Features.PreviewCommunication;

public record PreviewCommunicationQuery(
    string Rsc, string OrisCode, CommunicationContent Content)
    : IRequest<ErrorOr<GeneratedReport>>;
