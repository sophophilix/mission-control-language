# Engineering Methodology

FML does not stand alone. It is the language layer of a broader engineering approach developed from real-world LLM usage over six months across production debugging, infrastructure, Kubernetes/Helm operations, and software development across multiple languages and stacks.

## The core problem

Getting reliable output from an LLM requires decomposing the problem, identifying the relevant reasoning lenses, sequencing them deliberately, and structuring the handoff between them. Without that structure, agents work from vague instructions and produce inconsistent, hard-to-review output.

That process was always done manually — buried in ad-hoc prompts and markdown files. FML makes it explicit.

## The methodology

### 1. Design first
Never execute cold. Iterate on design with the LLM until it is solid before any implementation begins. FML is the language for expressing that design as a reasoning structure.

### 2. Phase decomposition
Break the design into agreed phases. Each phase is a meaningful, bounded unit of work with a clear completion condition.

### 3. Atomic task generation
Per phase, generate tasks in sequential dependency order so each task can be executed and tested independently before the next begins.

### 4. Narrow execution
By the time an agent executes, the work is so well-prescribed that there is little room for drift. The design thinking is already done. The agent follows the plan.

### 5. Oversight
An architect agent reviews the work of the executing agent. Catches omissions, enforces quality gates, ensures testing is done. Prevents the executing agent from convincing itself it is done when it is not.

### 6. Session continuity
Agent performance degrades as context fills — not just at the token limit, but before it. Sessions are treated as bounded units of work with structured handoffs:

- At session end: the agent writes a handoff capturing what is done, what is in progress, what is next, and any critical context.
- At session start: a fresh agent loads the handoff and the current plan state — not the full prior context.
- Result: sustained quality across long-running work without context degradation.

## Hub/spoke as "JIRA for LLMs"

The hub/spoke structure serves dual purpose:

- **Hub** (`docs/plan.md`): current status and active work — small, cheap to load, always current
- **Spokes** (`docs/phases/*.md`): detailed task lists and status per phase — loaded on demand
- **Archive**: completed work moves here to keep the hub minimal

This is not just documentation. It is the agent's source of truth for what is done and what remains at the start of every fresh session. It functions like a ticket system — checkpointing work progress so any agent, at any time, can pick up exactly where the last one left off.

## Where FML fits

FML addresses the design layer of this methodology: expressing the reasoning structure of a problem in a form that is explicit, composable, reviewable, and handoff-friendly. A mission defined in FML can be read by a human reviewer, an oversight agent, or a fresh executing agent — and the reasoning approach is immediately clear.
