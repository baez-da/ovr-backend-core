using System.ComponentModel.DataAnnotations;

namespace OVR.Api.Configuration;

public class MongoDbOptions
{
    public const string Section = "MongoDb";

    [Required(ErrorMessage = "ConnectionString is required")]
    public required string ConnectionString { get; init; }

    [Required(ErrorMessage = "DatabaseName is required")]
    public required string DatabaseName { get; init; }
}
