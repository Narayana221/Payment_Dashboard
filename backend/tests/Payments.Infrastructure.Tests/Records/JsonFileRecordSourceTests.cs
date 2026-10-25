using Microsoft.Extensions.Logging.Abstractions;
using Payments.Infrastructure.Options;
using Payments.Infrastructure.Records;
using Payments.Tests.Shared;
using Payments.Tests.Shared.Fixtures;

namespace Payments.Infrastructure.Tests.Records;

public sealed class JsonFileRecordSourceTests : IDisposable
{
    private const string Organiser = "acct_demo";

    private readonly string _tempDirectory = Directory.CreateTempSubdirectory("payments-tests-").FullName;

    public void Dispose() => Directory.Delete(_tempDirectory, recursive: true);

    [Fact]
    public async Task LoadAsync_ConfiguredOrganiser_ReadsTheFile()
    {
        using var source = Source(RealRecords.Path);

        var records = await source.LoadAsync(Organiser, CancellationToken.None);

        Assert.NotNull(records);
        Assert.Equal(GoldenFigures.UniqueCharges, records.Ledger.Charges.Count);
        Assert.Equal(2, records.ParseIssues.Count); // the two duplicates
    }

    [Fact]
    public async Task LoadAsync_UnknownOrganiser_ReturnsNullWithoutTouchingTheFile()
    {
        using var source = Source(Path.Combine(_tempDirectory, "does-not-exist.json"));

        Assert.Null(await source.LoadAsync("acct_someone_else", CancellationToken.None));
    }

    [Fact]
    public async Task LoadAsync_CalledTwice_ReadsTheFileOnce()
    {
        var path = Path.Combine(_tempDirectory, "records.json");
        File.Copy(RealRecords.Path, path);
        using var source = Source(path);

        var first = await source.LoadAsync(Organiser, CancellationToken.None);
        File.Delete(path);
        var second = await source.LoadAsync(Organiser, CancellationToken.None);

        Assert.Same(first, second);
    }

    [Fact]
    public async Task LoadAsync_FileOverSizeLimit_Throws()
    {
        using var source = Source(RealRecords.Path, maxFileBytes: 1_000);

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => source.LoadAsync(Organiser, CancellationToken.None));
        Assert.Contains("limit", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_MissingFile_Throws()
    {
        using var source = Source(Path.Combine(_tempDirectory, "missing.json"));

        await Assert.ThrowsAsync<FileNotFoundException>(() => source.LoadAsync(Organiser, CancellationToken.None));
    }

    private static JsonFileRecordSource Source(string path, long maxFileBytes = 10 * 1024 * 1024) =>
        new(
            Microsoft.Extensions.Options.Options.Create(new PaymentsOptions
            {
                OrganiserId = Organiser,
                RecordsPath = path,
                MaxFileBytes = maxFileBytes,
            }),
            NullLogger<JsonFileRecordSource>.Instance);
}
