using FluentAssertions;
using NSubstitute;
using MediatR;
using OVR.Ingestion.Features.ImportDtPartic;
using OVR.Modules.Entries.Domain;
using OVR.Modules.Entries.Persistence;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Domain.NameSystem;
using OVR.Modules.ParticipantRegistry.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Ingestion.Tests.Features.ImportDtPartic;

public class ImportDtParticHandlerTests
{
    private readonly IParticipantRepository _participantRepo = Substitute.For<IParticipantRepository>();
    private readonly IEntryRepository _entryRepo = Substitute.For<IEntryRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly INameBuilder _nameBuilder = new OdfNameBuilder();
    private readonly ImportDtParticHandler _handler;

    public ImportDtParticHandlerTests()
    {
        _participantRepo.FindByOdfDisciplineAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _handler = new ImportDtParticHandler(
            _participantRepo, _entryRepo, _publisher, _nameBuilder);
    }

    private static Stream GetTestXmlStream()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "valid-dt-partic.xml");
        var bytes = File.ReadAllBytes(path);
        return new MemoryStream(bytes);
    }

    [Fact]
    public async Task Handle_ValidXml_ShouldReturnSuccess()
    {
        using var stream = GetTestXmlStream();
        var command = new ImportDtParticCommand(stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Discipline.Should().Be("BOX-------------------------------");
        result.Value.Version.Should().Be(6);
    }

    [Fact]
    public async Task Handle_ValidXml_ShouldCreateParticipants()
    {
        using var stream = GetTestXmlStream();
        var command = new ImportDtParticCommand(stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Value.Participants.Created.Should().Be(3);
        await _participantRepo.Received(1).AddManyAsync(
            Arg.Is<IReadOnlyList<Participant>>(p => p.Count == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidXml_ShouldCreateEntriesOnlyForAthletes()
    {
        using var stream = GetTestXmlStream();
        var command = new ImportDtParticCommand(stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Value.Entries.Created.Should().Be(1);
        result.Value.Participants.Athletes.Should().Be(1);
        result.Value.Participants.Officials.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ValidXml_ShouldPublishDtParticImportedEvent()
    {
        using var stream = GetTestXmlStream();
        var command = new ImportDtParticCommand(stream);

        await _handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Any<OVR.SharedKernel.Domain.Events.Integration.DtParticImportedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Reimport_ShouldDeletePreviousData()
    {
        var existingParticipant = Participant.CreateFromOdf(
            "OLD001", BiographicData.Create(null, "Old", Gender.FromCode("M"), null, Organisation.Create("USA")),
            [ParticipantFunction.Create("JU", "BOX-------------------------------", true)],
            "p", "pi", "tv", "tvi", "tvf", "pscb", "pscbs", "pscbl",
            null, "ACTIVE", true, null, null, null, null);

        _participantRepo.FindByOdfDisciplineAsync("BOX-------------------------------", Arg.Any<CancellationToken>())
            .Returns([existingParticipant]);

        using var stream = GetTestXmlStream();
        var command = new ImportDtParticCommand(stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Value.PreviousImport.Existed.Should().BeTrue();
        result.Value.PreviousImport.ParticipantsRemoved.Should().Be(1);

        await _participantRepo.Received(1).DeleteManyAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidDocumentType_ShouldReturnError()
    {
        var xml = """<?xml version="1.0"?><OdfBody DocumentType="DT_RESULT"><Competition/></OdfBody>""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        var command = new ImportDtParticCommand(stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Ingestion.MalformedXml");
    }
}
