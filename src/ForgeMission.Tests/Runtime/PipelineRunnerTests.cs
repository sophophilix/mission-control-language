using ForgeMission.Core.Experts;
using ForgeMission.Core.Parser;
using ForgeMission.Core.Runtime;

namespace ForgeMission.Tests.Runtime;

public class PipelineRunnerTests : IDisposable
{
    private readonly string _outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public PipelineRunnerTests() => Directory.CreateDirectory(_outDir);

    public void Dispose() => Directory.Delete(_outDir, recursive: true);

    private static ExpertDefinition Expert(string name) =>
        new(name, "Input", "Output", $"You are {name}.");

    private static Dictionary<string, ExpertDefinition> Experts(params string[] names) =>
        names.ToDictionary(n => n, Expert, StringComparer.Ordinal);

    [Fact]
    public async Task SingleStep_CallsRunnerOnce_WritesOutput()
    {
        var ast     = FmlParser.Parse("mission BuildOperator = KubernetesArchitect");
        var stub    = new StubExpertRunner();
        var runner  = new PipelineRunner(stub);
        var options = new PipelineRunOptions("BuildOperator", "initial input", _outDir);

        await runner.RunAsync(ast, Experts("KubernetesArchitect"), options);

        Assert.Single(stub.Calls);
        Assert.Equal("KubernetesArchitect", stub.Calls[0].ExpertName);
    }

    [Fact]
    public async Task MultiStep_PassesContextForward()
    {
        var ast = FmlParser.Parse("""
            mission BuildOperator =
                KubernetesArchitect
                |> SecurityArchitect
                |> PrincipalReviewer
            """);

        var stub = new StubExpertRunner((name, ctx) => $"Output from {name} given [{ctx}]");
        var runner = new PipelineRunner(stub);
        var options = new PipelineRunOptions("BuildOperator", "initial input", _outDir);

        await runner.RunAsync(ast, Experts("KubernetesArchitect", "SecurityArchitect", "PrincipalReviewer"), options);

        Assert.Equal(3, stub.Calls.Count);
        Assert.Equal("initial input",                                  stub.Calls[0].Context);
        Assert.Equal("Output from KubernetesArchitect given [initial input]", stub.Calls[1].Context);
        Assert.Equal("Output from SecurityArchitect given [Output from KubernetesArchitect given [initial input]]", stub.Calls[2].Context);
    }

    [Fact]
    public async Task OutputFiles_HaveCorrectNamesAndNumbering()
    {
        var ast = FmlParser.Parse("""
            mission BuildOperator =
                KubernetesArchitect
                |> SecurityArchitect
                |> PrincipalReviewer
            """);

        await new PipelineRunner(new StubExpertRunner())
            .RunAsync(ast, Experts("KubernetesArchitect", "SecurityArchitect", "PrincipalReviewer"),
                      new PipelineRunOptions("BuildOperator", "input", _outDir));

        var runDir = Path.Combine(_outDir, "BuildOperator");
        Assert.True(File.Exists(Path.Combine(runDir, "01-KubernetesArchitect.md")));
        Assert.True(File.Exists(Path.Combine(runDir, "02-SecurityArchitect.md")));
        Assert.True(File.Exists(Path.Combine(runDir, "03-PrincipalReviewer.md")));
    }

    [Fact]
    public async Task FinalMd_MatchesLastStepOutput()
    {
        var ast = FmlParser.Parse("""
            mission BuildOperator =
                KubernetesArchitect
                |> PrincipalReviewer
            """);

        var stub = new StubExpertRunner((name, _) => $"Output from {name}");
        await new PipelineRunner(stub)
            .RunAsync(ast, Experts("KubernetesArchitect", "PrincipalReviewer"),
                      new PipelineRunOptions("BuildOperator", "input", _outDir));

        var finalContent = await File.ReadAllTextAsync(Path.Combine(_outDir, "BuildOperator", "final.md"));
        Assert.Equal("Output from PrincipalReviewer", finalContent);
    }

    [Fact]
    public async Task CancellationToken_IsRespected()
    {
        var ast = FmlParser.Parse("""
            mission BuildOperator =
                KubernetesArchitect
                |> SecurityArchitect
            """);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new PipelineRunner(new StubExpertRunner())
                .RunAsync(ast, Experts("KubernetesArchitect", "SecurityArchitect"),
                          new PipelineRunOptions("BuildOperator", "input", _outDir),
                          cts.Token));
    }
}
