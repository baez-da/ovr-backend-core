using Microsoft.Extensions.DependencyInjection;

namespace OVR.SharedKernel.Abstractions;

public interface IModuleInstaller
{
    IServiceCollection AddModule(IServiceCollection services);
}
