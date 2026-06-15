using ForgeMission.Core.Experts;
using ForgeMission.Core.Parser;

namespace ForgeMission.Core.Runtime;

public class PipelineRunner(IExpertRunner expertRunner)
{
    public async Task<MissionResult> RunAsync(
        Program ast,
        Dictionary<string, ExpertDefinition> experts,
        PipelineRunOptions options,
        CancellationToken ct = default)
    {
        var mission = ast.Declarations
            .OfType<MissionDeclaration>()
            .FirstOrDefault(m => m.Name == options.MissionName)
            ?? throw new InvalidOperationException(
                $"Mission '{options.MissionName}' not found in .fml file");

        var context = SeedContext(ast, options);
        var steps   = Flatten(mission.Pipeline, ast);

        foreach (var step in steps)
        {
            ct.ThrowIfCancellationRequested();

            if (!experts.TryGetValue(step.ExpertName, out var expert))
                throw new InvalidOperationException(
                    $"Expert '{step.ExpertName}' not found. " +
                    "Run 'fml validate' to check your mission before running.");

            foreach (var binding in step.With)
                context[binding.Key] = ResolveBindingValue(binding.Value, context);

            if (options.StepWriter is { } sw)
                await sw.WriteLineAsync($"→ {step.ExpertName}...");

            var output = await expertRunner.RunAsync(expert, context, ct);

            if (options.StepWriter is { } sw2)
                await sw2.WriteLineAsync($"{output}\n");

            context["output"] = output;
        }

        var text = context.TryGetValue("output", out var last) ? last.ToString()! : string.Empty;
        return new MissionResult(options.MissionName, text);
    }

    private static Dictionary<string, object> SeedContext(Program ast, PipelineRunOptions options)
    {
        var context = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var binding in ast.Bindings)
            context[binding.Name] = ResolveLetValue(binding.Value, binding.Name);

        context["output"] = string.Empty;

        if (options.Vars is { } vars)
            foreach (var (key, value) in vars)
                context[key] = value;

        return context;
    }

    private static string ResolveLetValue(LetValue value, string bindingName) => value switch
    {
        StringLetValue v => v.Text,
        EnvLetValue v    => ResolveEnv(v.VarName, v.DefaultValue, bindingName),
        _                => throw new InvalidOperationException($"Unknown let value type for '{bindingName}'")
    };

    private static string ResolveEnv(string varName, string? defaultValue, string bindingName)
    {
        var val = Environment.GetEnvironmentVariable(varName);
        if (val is not null) return val;
        if (defaultValue is not null) return defaultValue;
        throw new InvalidOperationException(
            $"Required environment variable '{varName}' (used by let binding '{bindingName}') is not set.");
    }

    private static string ResolveBindingValue(BindingValue value, Dictionary<string, object> context) => value switch
    {
        StringBindingValue v => v.Text,
        VarRefBindingValue v => context.TryGetValue(v.Name, out var ctx)
            ? ctx.ToString()!
            : throw new InvalidOperationException($"Variable '{v.Name}' not found in context"),
        EnvBindingValue v    => ResolveEnv(v.VarName, v.DefaultValue, v.VarName),
        _                    => throw new InvalidOperationException("Unknown binding value type")
    };

    private static List<Step> Flatten(Pipeline pipeline, Program ast)
    {
        var expertDecls = ast.Declarations
            .OfType<ExpertDeclaration>()
            .ToDictionary(e => e.Name, StringComparer.Ordinal);

        var result = new List<Step>();
        foreach (var step in pipeline.Steps)
            FlattenStep(step, expertDecls, result, []);

        return result;
    }

    private static void FlattenStep(
        Step step,
        Dictionary<string, ExpertDeclaration> decls,
        List<Step> result,
        HashSet<string> visited)
    {
        if (!visited.Add(step.ExpertName))
            throw new InvalidOperationException(
                $"Circular expert reference detected: '{step.ExpertName}'");

        if (decls.TryGetValue(step.ExpertName, out var decl))
        {
            foreach (var inner in decl.Pipeline.Steps)
                FlattenStep(inner, decls, result, new HashSet<string>(visited, StringComparer.Ordinal));
        }
        else
        {
            result.Add(step);
        }
    }
}
