using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.CommonCodes.Features.BulkGetCommonCodes;
using OVR.Modules.CommonCodes.Features.GetCommonCodes;
using OVR.Modules.CommonCodes.Features.ImportFromExcel;
using OVR.Modules.CommonCodes.Features.ListCommonCodeTypes;
using OVR.Modules.CommonCodes.Persistence;
using OVR.Modules.CommonCodes.Services;
using OVR.SharedKernel.Contracts;

namespace OVR.Modules.CommonCodes;

public static class CommonCodesModule
{
    public static IServiceCollection AddCommonCodesModule(this IServiceCollection services)
    {
        services.AddScoped<ICommonCodeRepository, MongoCommonCodeRepository>();
        services.AddSingleton<CommonCodeCacheService>();
        services.AddSingleton<ICommonCodeCache>(sp => sp.GetRequiredService<CommonCodeCacheService>());
        services.AddHostedService(sp => sp.GetRequiredService<CommonCodeCacheService>());
        return services;
    }

    public static IEndpointRouteBuilder MapCommonCodesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapImportFromExcelEndpoint();
        app.MapGetCommonCodesEndpoint();
        app.MapListCommonCodeTypesEndpoint();
        app.MapBulkGetCommonCodesEndpoint();
        return app;
    }
}
