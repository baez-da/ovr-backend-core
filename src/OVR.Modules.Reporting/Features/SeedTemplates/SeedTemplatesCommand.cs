using ErrorOr;
using MediatR;

namespace OVR.Modules.Reporting.Features.SeedTemplates;

public record SeedTemplatesCommand : IRequest<ErrorOr<SeedTemplatesResponse>>;

public record SeedTemplatesResponse(int Processed);
