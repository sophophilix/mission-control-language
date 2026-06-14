using ForgeMission.Core.Experts;
using ForgeMission.Core.Parser;

namespace ForgeMission.Core.Runtime;

public class PipelineRunner(IExpertRunner expertRunner)
{
    public async Task RunAsync(
        Program ast,
        Dictionary<string, ExpertDefinition> experts,
        PipelineRunOptions options,
        CancellationToken ct = default)
    {
        var mission = ast.Declarations
            .OfType<MissionDeclaration>()
            .FirstOrDefault(m => m.Name == options.MissionName)
            ?? throw new InvalidOperationException($"Mission '{options.MissionName}' not found in .fml file");

        var steps = Flatten(mission.Pipeline, ast);

        var runDir = Path.Combine(options.OutputDirectory, options.MissionName);
        Directory.CreateDirectory(runDir);

        var context = options.InputText;
        var stepNumber = 1;

        foreach (var stepName in steps)
        {
            ct.ThrowIfCancellationRequested();

            if (!experts.TryGetValue(stepName, out var expert))
                throw new InvalidOperationException(
                    $"Expert '{stepName}' not found. Run 'fml validate' to check your mission before running.");

            var output = await expertRunner.RunAsync(expert, context, ct);

            var filename = $"{stepNumber:D2}-{stepName}.md";
            await File.WriteAllTextAsync(Path.Combine(runDir, filename), output, ct);

            context = output;
            stepNumber++;
        }

        await File.WriteAllTextAsync(Path.Combine(runDir, "final.md"), context, ct);
    }

    private static List<string> Flatten(Pipeline pipeline, Program ast)
    {
        var expertDecls = ast.Declarations
            .OfType<ExpertDeclaration>()
            .ToDictionary(e => e.Name, StringComparer.Ordinal);

        var result = new List<string>();
        foreach (var step in pipeline.Steps)
            FlattenStep(step, expertDecls, result, []);

        return result;
    }

    private static void FlattenStep(
        string name,
        Dictionary<string, ExpertDeclaration> decls,
        List<string> result,
        HashSet<string> visited)
    {
        if (!visited.Add(name))
            throw new InvalidOperationException($"Circular expert reference detected: '{name}'");

        if (decls.TryGetValue(name, out var decl))
        {
            foreach (var step in decl.Pipeline.Steps)
                FlattenStep(step, decls, result, new HashSet<string>(visited, StringComparer.Ordinal));
        }
        else
        {
            result.Add(name);
        }
    }
}
