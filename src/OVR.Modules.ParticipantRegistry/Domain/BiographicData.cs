using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Domain;

public sealed class BiographicData : ValueObject
{
    public string? GivenName { get; }
    public string FamilyName { get; }
    public Gender Gender { get; }
    public DateOnly? BirthDate { get; }
    public Organisation Organisation { get; }

    private BiographicData(string? givenName, string familyName, Gender gender, DateOnly? birthDate, Organisation organisation)
    {
        GivenName = givenName;
        FamilyName = familyName;
        Gender = gender;
        BirthDate = birthDate;
        Organisation = organisation;
    }

    public static BiographicData Create(string? givenName, string familyName, Gender gender, DateOnly? birthDate, Organisation organisation)
    {
        if (givenName is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(givenName);
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentNullException.ThrowIfNull(gender);
        ArgumentNullException.ThrowIfNull(organisation);
        return new BiographicData(givenName?.Trim(), familyName.Trim(), gender, birthDate, organisation);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return GivenName ?? string.Empty;
        yield return FamilyName;
        yield return Gender;
        yield return BirthDate;
        yield return Organisation;
    }
}
