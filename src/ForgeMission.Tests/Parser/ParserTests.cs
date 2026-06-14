using ForgeMission.Core.Parser;

namespace ForgeMission.Tests.Parser;

public class ParserTests
{
    [Fact]
    public void ValidMission_SingleExpert_ParsesCorrectly()
    {
        var result = FmlParser.Parse("mission BuildOperator = KubernetesArchitect");

        var mission = Assert.Single(result.Declarations) as MissionDeclaration;
        Assert.NotNull(mission);
        Assert.Equal("BuildOperator", mission.Name);
        Assert.Equal(["KubernetesArchitect"], mission.Pipeline.Steps.Select(s => s.ExpertName));
    }

    [Fact]
    public void ValidMission_MultiStepPipeline_ParsesCorrectly()
    {
        var source = """
            mission BuildOperator =
                KubernetesArchitect
                |> SecurityArchitect
                |> PrincipalReviewer
            """;

        var result = FmlParser.Parse(source);

        var mission = Assert.Single(result.Declarations) as MissionDeclaration;
        Assert.NotNull(mission);
        Assert.Equal("BuildOperator", mission.Name);
        Assert.Equal(
            ["KubernetesArchitect", "SecurityArchitect", "PrincipalReviewer"],
            mission.Pipeline.Steps.Select(s => s.ExpertName));
    }

    [Fact]
    public void ValidExpert_ParsesCorrectly()
    {
        var source = """
            expert KubernetesArchitect =
                RequirementsAnalyst
                |> PlatformArchitect
                |> ReliabilityArchitect
            """;

        var result = FmlParser.Parse(source);

        var expert = Assert.Single(result.Declarations) as ExpertDeclaration;
        Assert.NotNull(expert);
        Assert.Equal("KubernetesArchitect", expert.Name);
        Assert.Equal(
            ["RequirementsAnalyst", "PlatformArchitect", "ReliabilityArchitect"],
            expert.Pipeline.Steps.Select(s => s.ExpertName));
    }

    [Fact]
    public void RecursiveExpert_ReferencingOtherExperts_ParsesCorrectly()
    {
        var source = """
            expert KubernetesArchitect =
                RequirementsAnalyst
                |> PlatformArchitect

            mission BuildOperator =
                KubernetesArchitect
                |> SecurityArchitect
            """;

        var result = FmlParser.Parse(source);

        Assert.Equal(2, result.Declarations.Count);
        Assert.IsType<ExpertDeclaration>(result.Declarations[0]);
        Assert.IsType<MissionDeclaration>(result.Declarations[1]);
    }

    [Fact]
    public void MissionAndExpert_InSameFile_ParsesCorrectly()
    {
        var source = """
            mission BuildOperator =
                KubernetesArchitect
                |> SecurityArchitect
                |> PrincipalReviewer

            expert KubernetesArchitect =
                RequirementsAnalyst
                |> PlatformArchitect
            """;

        var result = FmlParser.Parse(source);

        Assert.Equal(2, result.Declarations.Count);
        var mission = result.Declarations[0] as MissionDeclaration;
        var expert = result.Declarations[1] as ExpertDeclaration;
        Assert.NotNull(mission);
        Assert.NotNull(expert);
        Assert.Equal("BuildOperator", mission.Name);
        Assert.Equal("KubernetesArchitect", expert.Name);
    }

    [Fact]
    public void LowercaseIdentifier_ThrowsParseException()
    {
        var source = "mission BuildOperator = kubernetesArchitect";

        var ex = Assert.Throws<ParseException>(() => FmlParser.Parse(source));
        Assert.Contains("PascalCase", ex.Message);
    }

    [Fact]
    public void MissingEquals_ThrowsParseException()
    {
        var source = "mission BuildOperator KubernetesArchitect";

        var ex = Assert.Throws<ParseException>(() => FmlParser.Parse(source));
        Assert.Contains("'='", ex.Message);
    }

    [Fact]
    public void EmptyPipeline_ThrowsParseException()
    {
        var source = "mission BuildOperator =";

        Assert.Throws<ParseException>(() => FmlParser.Parse(source));
    }

    [Fact]
    public void LetBinding_StringLiteral_ParsesCorrectly()
    {
        var source = """
            let goal = "Design a K8s operator"
            mission BuildOperator = KubernetesArchitect
            """;

        var result = FmlParser.Parse(source);

        Assert.Single(result.Bindings);
        var binding = result.Bindings[0];
        Assert.Equal("goal", binding.Name);
        var value = Assert.IsType<StringLetValue>(binding.Value);
        Assert.Equal("Design a K8s operator", value.Text);
    }

    [Fact]
    public void LetBinding_EnvCall_ParsesCorrectly()
    {
        var source = """
            let apiKey = env("OPENAI_API_KEY")
            mission BuildOperator = KubernetesArchitect
            """;

        var result = FmlParser.Parse(source);

        var binding = Assert.Single(result.Bindings);
        var value = Assert.IsType<EnvLetValue>(binding.Value);
        Assert.Equal("OPENAI_API_KEY", value.VarName);
        Assert.Null(value.DefaultValue);
    }

    [Fact]
    public void LetBinding_EnvCallWithDefault_ParsesCorrectly()
    {
        var source = """
            let model = env("FML_MODEL", "gpt-4o-mini")
            mission BuildOperator = KubernetesArchitect
            """;

        var result = FmlParser.Parse(source);

        var binding = Assert.Single(result.Bindings);
        var value = Assert.IsType<EnvLetValue>(binding.Value);
        Assert.Equal("FML_MODEL", value.VarName);
        Assert.Equal("gpt-4o-mini", value.DefaultValue);
    }

    [Fact]
    public void MissionParams_ParseCorrectly()
    {
        var source = """
            mission BuildOperator(goal, persona) =
                KubernetesArchitect
            """;

        var result = FmlParser.Parse(source);

        var mission = Assert.Single(result.Declarations) as MissionDeclaration;
        Assert.NotNull(mission);
        Assert.Equal(["goal", "persona"], mission.Params);
    }

    [Fact]
    public void WithClause_ParsesCorrectly()
    {
        var source = """
            mission BuildOperator =
                KubernetesArchitect
                |> PrincipalReviewer with { style = "terse ADR" }
            """;

        var result = FmlParser.Parse(source);

        var mission = Assert.Single(result.Declarations) as MissionDeclaration;
        Assert.NotNull(mission);
        var lastStep = mission.Pipeline.Steps[1];
        Assert.Equal("PrincipalReviewer", lastStep.ExpertName);
        var binding = Assert.Single(lastStep.With);
        Assert.Equal("style", binding.Key);
        var value = Assert.IsType<StringBindingValue>(binding.Value);
        Assert.Equal("terse ADR", value.Text);
    }

    [Fact]
    public void WithClause_VarRef_ParsesCorrectly()
    {
        var source = """
            let myStyle = "verbose"
            mission BuildOperator =
                KubernetesArchitect with { style = myStyle }
            """;

        var result = FmlParser.Parse(source);

        var mission = Assert.Single(result.Declarations) as MissionDeclaration;
        Assert.NotNull(mission);
        var step = Assert.Single(mission.Pipeline.Steps);
        var binding = Assert.Single(step.With);
        var value = Assert.IsType<VarRefBindingValue>(binding.Value);
        Assert.Equal("myStyle", value.Name);
    }
}
