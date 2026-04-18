using OVR.SharedKernel.Domain.Primitives;

namespace OVR.Modules.Scheduling.Domain;

public sealed class Session : AggregateRoot<string>
{
    public string Code { get; private set; } = string.Empty;
    public string VenueCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public TimeSpan? Leadin { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Session() { }

    public static Session Create(
        string code,
        string venueCode,
        string name,
        DateTime startDate,
        DateTime endDate,
        TimeSpan? leadin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(venueCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (venueCode.Length != 3)
            throw new ArgumentException(
                $"VenueCode must be exactly 3 characters, got '{venueCode}'.",
                nameof(venueCode));

        if (endDate <= startDate)
            throw new ArgumentException(
                $"EndDate ({endDate:O}) must be strictly greater than StartDate ({startDate:O}).",
                nameof(endDate));

        if (leadin.HasValue && leadin.Value < TimeSpan.Zero)
            throw new ArgumentException(
                $"Leadin must be non-negative, got {leadin.Value}.",
                nameof(leadin));

        return new Session
        {
            Id = code,
            Code = code,
            VenueCode = venueCode,
            Name = name,
            StartDate = startDate,
            EndDate = endDate,
            Leadin = leadin,
            CreatedAt = DateTime.UtcNow
        };
    }
}
