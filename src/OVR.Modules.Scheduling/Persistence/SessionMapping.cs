using OVR.Modules.Scheduling.Domain;

namespace OVR.Modules.Scheduling.Persistence;

internal static class SessionMapping
{
    public static SessionDocument ToDocument(Session session) => new()
    {
        Id = session.Id,
        VenueCode = session.VenueCode,
        Name = session.Name,
        StartDate = session.StartDate,
        EndDate = session.EndDate,
        Leadin = session.Leadin,
        CreatedAt = session.CreatedAt
    };

    public static Session ToDomain(SessionDocument doc) =>
        Session.Create(doc.Id, doc.VenueCode, doc.Name, doc.StartDate, doc.EndDate, doc.Leadin);
}
