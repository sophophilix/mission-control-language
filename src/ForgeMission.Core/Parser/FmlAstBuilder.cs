using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace ForgeMission.Core.Parser;

internal class FmlAstBuilder : FmlGrammarBaseVisitor<object?>
{
    public override object? VisitProgram(FmlGrammarParser.ProgramContext ctx)
    {
        var declarations = ctx.declaration()
            .Select(d => (Declaration)Visit(d)!)
            .ToList();
        return new Program(declarations);
    }

    public override object? VisitDeclaration(FmlGrammarParser.DeclarationContext ctx)
    {
        if (ctx.mission() is { } m) return Visit(m);
        if (ctx.expert() is { } e) return Visit(e);
        throw new ParseException("Unknown declaration", ctx.Start.Line, ctx.Start.Column);
    }

    public override object? VisitMission(FmlGrammarParser.MissionContext ctx)
    {
        var name     = ctx.UPPER_ID().GetText();
        var pipeline = (Pipeline)Visit(ctx.pipeline())!;
        return new MissionDeclaration(name, pipeline);
    }

    public override object? VisitExpert(FmlGrammarParser.ExpertContext ctx)
    {
        var name     = ctx.UPPER_ID().GetText();
        var pipeline = (Pipeline)Visit(ctx.pipeline())!;
        return new ExpertDeclaration(name, pipeline);
    }

    public override object? VisitPipeline(FmlGrammarParser.PipelineContext ctx)
    {
        var steps = ctx.step().Select(s => s.UPPER_ID().GetText()).ToList();
        return new Pipeline(steps);
    }
}
