using System.CommandLine;
using ForgeMission.Core.Adapters;
using ForgeMission.Core.Experts;
using ForgeMission.Core.Parser;
using ForgeMission.Core.Runtime;
using Microsoft.Extensions.AI;
using OpenAI;
using FmlProgram = ForgeMission.Core.Parser.Program;

var rootCommand = new RootCommand("fml — Forge Mission Language runtime");
rootCommand.Add(BuildRunCommand());
rootCommand.Add(BuildValidateCommand());
rootCommand.Add(BuildListCommand());

return await rootCommand.Parse(args).InvokeAsync();

// ---------------------------------------------------------------------------

static Command BuildRunCommand()
{
    var missionArg = new Argument<FileInfo>("mission") { Description = "Path to the .fml mission file" };
    var inputOpt   = new Option<FileInfo>("--input") { Description = "Path to the input markdown file", Required = true };
    var expertsOpt = new Option<DirectoryInfo?>("--experts") { Description = "Experts directory (default: <mission-dir>/experts)" };
    var outputOpt  = new Option<DirectoryInfo?>("--output") { Description = "Output directory (default: ./runs)" };

    var cmd = new Command("run", "Run a mission against an input");
    cmd.Add(missionArg);
    cmd.Add(inputOpt);
    cmd.Add(expertsOpt);
    cmd.Add(outputOpt);

    cmd.SetAction(async result =>
    {
        var mission  = result.GetValue(missionArg)!;
        var input    = result.GetValue(inputOpt)!;
        var experts  = result.GetValue(expertsOpt);
        var output   = result.GetValue(outputOpt);

        var expertsDir = experts?.FullName ?? Path.Combine(mission.DirectoryName!, "experts");
        var outputDir  = output?.FullName ?? "runs";

        var source = await TryReadFile(mission.FullName);
        if (source is null) return;

        var inputText = await TryReadFile(input.FullName);
        if (inputText is null) return;

        var ast = TryParse(source);
        if (ast is null) return;

        var expertDefs = TryLoadExperts(expertsDir);
        if (expertDefs is null) return;

        if (!TryValidate(ast, expertDefs)) return;

        var runner = TryBuildRunner();
        if (runner is null) return;

        var firstMission = ast.Declarations.OfType<MissionDeclaration>().FirstOrDefault();
        if (firstMission is null) { Die("No mission declaration found in mission file."); return; }

        var options = new PipelineRunOptions(firstMission.Name, inputText, outputDir);
        Console.WriteLine($"Running mission '{firstMission.Name}'...");

        await new PipelineRunner(runner).RunAsync(ast, expertDefs, options);

        Console.WriteLine($"Done. Output: {Path.Combine(outputDir, firstMission.Name, "final.md")}");
    });

    return cmd;
}

static Command BuildValidateCommand()
{
    var missionArg = new Argument<FileInfo>("mission") { Description = "Path to the .fml mission file" };
    var expertsOpt = new Option<DirectoryInfo?>("--experts") { Description = "Experts directory (default: <mission-dir>/experts)" };

    var cmd = new Command("validate", "Validate a mission file and its expert references");
    cmd.Add(missionArg);
    cmd.Add(expertsOpt);

    cmd.SetAction(async result =>
    {
        var mission  = result.GetValue(missionArg)!;
        var experts  = result.GetValue(expertsOpt);

        var expertsDir = experts?.FullName ?? Path.Combine(mission.DirectoryName!, "experts");

        var source = await TryReadFile(mission.FullName);
        if (source is null) return;

        var ast = TryParse(source);
        if (ast is null) return;

        var expertDefs = TryLoadExperts(expertsDir);
        if (expertDefs is null) return;

        if (TryValidate(ast, expertDefs))
            Console.WriteLine("OK — mission is valid.");
    });

    return cmd;
}

static Command BuildListCommand()
{
    var listCommand = new Command("list", "List available resources");

    var expertsOpt = new Option<DirectoryInfo?>("--experts") { Description = "Experts directory (default: ./experts)" };
    var listExpertsCmd = new Command("experts", "List experts in the experts directory");
    listExpertsCmd.Add(expertsOpt);

    listExpertsCmd.SetAction(async result =>
    {
        var experts    = result.GetValue(expertsOpt);
        var expertsDir = experts?.FullName ?? "experts";

        var expertDefs = TryLoadExperts(expertsDir);
        if (expertDefs is null) return;

        if (expertDefs.Count == 0) { Console.WriteLine($"No experts found in {expertsDir}"); return; }

        Console.WriteLine($"Experts in {expertsDir}:");
        foreach (var (name, def) in expertDefs.OrderBy(k => k.Key))
            Console.WriteLine($"  {name}  ({def.Input} -> {def.Output})");

        await Task.CompletedTask;
    });

    listCommand.Add(listExpertsCmd);
    return listCommand;
}

// ---------------------------------------------------------------------------

static async Task<string?> TryReadFile(string path)
{
    try { return await File.ReadAllTextAsync(path); }
    catch (Exception ex) { Die($"Cannot read file '{path}': {ex.Message}"); return null; }
}

static FmlProgram? TryParse(string source)
{
    try { return FmlParser.Parse(source); }
    catch (ParseException ex) { Die(ex.Message); return null; }
}

static Dictionary<string, ExpertDefinition>? TryLoadExperts(string expertsDir)
{
    if (!Directory.Exists(expertsDir)) { Die($"Experts directory not found: {expertsDir}"); return null; }
    try { return new ExpertLoader(expertsDir).LoadAll(); }
    catch (ExpertLoadException ex) { Die(ex.Message); return null; }
}

static bool TryValidate(FmlProgram ast, Dictionary<string, ExpertDefinition> experts)
{
    try { ExpertLoader.Validate(ast, experts); return true; }
    catch (ExpertLoadException ex) { Die(ex.Message); return false; }
}

static IExpertRunner? TryBuildRunner()
{
    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey)) { Die("OPENAI_API_KEY environment variable is not set."); return null; }
    var chatClient = new OpenAIClient(apiKey).GetChatClient("gpt-4o-mini").AsIChatClient();
    return new MafExpertRunner(chatClient);
}

static void Die(string message)
{
    Console.Error.WriteLine($"error: {message}");
    Environment.Exit(1);
}
