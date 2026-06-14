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
    public async Task MultiStep_PassesOutputForward()
    {
        var ast = FmlParser.Parse("""
            mission BuildOperator =
                KubernetesArchitect
                |> SecurityArchitect
                |> PrincipalReviewer
            """);

        var stub = new StubExpertRunner((name, ctx) =>
            $"Output from {name} given [{ctx["output"]}]");
        var runner = new PipelineRunner(stub);
        var options = new PipelineRunOptions("BuildOperator", "initial input", _outDir);

        await runner.RunAsync(ast, Experts("KubernetesArchitect", "SecurityArchitect", "PrincipalReviewer"), options);

        Assert.Equal(3, stub.Calls.Count);
        Assert.Equal("initial input",
            stub.Calls[0].Context["output"].ToString());
        Assert.Equal("Output from KubernetesArchitect given [initial input]",
            stub.Calls[1].Context["output"].ToString());
        Assert.Equal("Output from SecurityArchitect given [Output from KubernetesArchitect given [initial input]]",
            stub.Calls[2].Context["output"].ToString());
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

    [Fact]
    public async Task LetBindings_SeedContext()
    {
        var ast = FmlParser.Parse("""
            let goal = "Design a K8s operator"
            mission BuildOperator = KubernetesArchitect
            """);

        var stub = new StubExpertRunner();
        await new PipelineRunner(stub)
            .RunAsync(ast, Experts("KubernetesArchitect"),
                      new PipelineRunOptions("BuildOperator", "input", _outDir));

        Assert.Equal("Design a K8s operator", stub.Calls[0].Context["goal"].ToString());
    }

    [Fact]
    public async Task WithClause_OverridesContextForStep()
    {
        var ast = FmlParser.Parse("""
            mission BuildOperator =
                KubernetesArchitect with { style = "terse" }
            """);

        var stub = new StubExpertRunner();
        await new PipelineRunner(stub)
            .RunAsync(ast, Experts("KubernetesArchitect"),
                      new PipelineRunOptions("BuildOperator", "input", _outDir));

        Assert.Equal("terse", stub.Calls[0].Context["style"].ToString());
    }

    [Fact]
    public async Task VarFlag_OverridesLetBinding()
    {
        var ast = FmlParser.Parse("""
            let goal = "original"
            mission BuildOperator = KubernetesArchitect
            """);

        var stub = new StubExpertRunner();
        var vars = new Dictionary<string, string> { ["goal"] = "overridden" };
        await new PipelineRunner(stub)
            .RunAsync(ast, Experts("KubernetesArchitect"),
                      new PipelineRunOptions("BuildOperator", "input", _outDir, vars));

        Assert.Equal("overridden", stub.Calls[0].Context["goal"].ToString());
    }

    [Fact]
    public void MissingEnvVar_ThrowsClearly()
    {
        var ast = FmlParser.Parse("""
            let apiKey = env("FMLTEST_MISSING_VAR_XYZ")
            mission BuildOperator = KubernetesArchitect
            """);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new PipelineRunner(new StubExpertRunner())
                .RunAsync(ast, Experts("KubernetesArchitect"),
                          new PipelineRunOptions("BuildOperator", "input", _outDir))
                .GetAwaiter().GetResult());

        Assert.Contains("FMLTEST_MISSING_VAR_XYZ", ex.Message);
    }
}
