using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.CommonCodes.Features.GetCommonCodes;
using OVR.Modules.CommonCodes.Features.ImportFromExcel;
using OVR.Modules.CommonCodes.Persistence;

namespace OVR.Modules.CommonCodes;

public static class CommonCodesModule
{
    public static IServiceCollection AddCommonCodesModule(this IServiceCollection services)
    {
        services.AddScoped<ICommonCodeRepository, MongoCommonCodeRepository>();
        services.AddScoped<ICommonCodeReader, MongoCommonCodeRepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapCommonCodesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapImportFromExcelEndpoint();
        app.MapGetCommonCodesEndpoint();
        return app;
    }
}
