# Phase 24 — Spoke 1: Test Harness

**Status:** Planned  
**Hub:** [phase-24-copilot-sdk-integration-tests.md](phase-24-copilot-sdk-integration-tests.md)  
**Parallel with:** nothing — Spokes 2 and 3 depend on this

## Goal

Build the shared infrastructure that all Phase 24 tests depend on:
- `InMemorySessionStore` — keeps sessions in RAM, no disk I/O
- `OaiServerFixture` — starts `OaiServer` on a random port, tears it down cleanly
- No-op mission files — a checked-in `.mcl` + `mcl.lock` usable by any test

---

## Tasks

### 1. Add `GitHub.Copilot.SDK` to `ForgeMission.Tests`

File: `src/ForgeMission.Tests/ForgeMission.Tests.csproj`

```xml
<PackageReference Include="GitHub.Copilot.SDK" Version="*" />
```

Verify `dotnet build` passes after adding.

---

### 2. Create `InMemorySessionStore`

File: `src/ForgeMission.Tests/Integration/InMemorySessionStore.cs`

```csharp
using System.Collections.Concurrent;
using ForgeMission.Core.Adapters; // or Katasec.OaiServer — wherever ISessionStore lives

namespace ForgeMission.Tests.Integration;

internal sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    public Task<Session?> GetAsync(string sessionId, CancellationToken ct = default)
        => Task.FromResult(_sessions.TryGetValue(sessionId, out var s) ? s : null);

    public Task SaveAsync(Session session, CancellationToken ct = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string sessionId, CancellationToken ct = default)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}
```

---

### 3. Create `OaiServerFixture`

File: `src/ForgeMission.Tests/Integration/OaiServerFixture.cs`

Starts `OaiServer` on a random free port. Exposes the base URL for clients.
Implements `IAsyncDisposable` so xUnit `await using` disposes it cleanly.

```csharp
using System.Net;
using System.Net.Sockets;
using Katasec.OaiServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ForgeMission.Tests.Integration;

internal sealed class OaiServerFixture : IAsyncDisposable
{
    private readonly WebApplication _app;

    public string BaseUrl { get; }
    public int Port { get; }

    private OaiServerFixture(WebApplication app, int port)
    {
        _app    = app;
        Port    = port;
        BaseUrl = $"http://localhost:{port}";
    }

    public static async Task<OaiServerFixture> StartAsync(
        IChatClient chatClient,
        string agentId = "test-agent",
        ISessionStore? sessionStore = null)
    {
        var port  = FindFreePort();
        var store = sessionStore ?? new InMemorySessionStore();

        var server  = new OaiServer(chatClient, store, agentId);
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseSetting("urls", $"http://127.0.0.1:{port}");
        builder.Logging.ClearProviders(); // keep test output clean

        var app = builder.Build();
        server.Map(app);

        await app.StartAsync();
        return new OaiServerFixture(app, port);
    }

    public async ValueTask DisposeAsync() => await _app.StopAsync();

    private static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
```

---

### 4. Create no-op mission files

#### `src/ForgeMission.Tests/Missions/noop/mission.mcl`

```
let apiKey  = env("MCL_API_KEY")
let model   = env("MCL_MODEL", "gpt-4o-mini")
let provider = env("MCL_PROVIDER", "openai")

use PassThrough

mission Noop(goal) {
    PassThrough
}
```

#### `src/ForgeMission.Tests/Missions/noop/experts/PassThrough/expert.md`

```markdown
---
name: PassThrough
version: 0.1.0
description: Returns the user input unchanged
input: any text
output: same text
---

You are a pass-through assistant. Return the user's message exactly as given,
without modification, commentary, or additions.
```

#### `src/ForgeMission.Tests/Missions/noop/mcl.lock`

```yaml
experts:
  PassThrough:
    source: local
    path: experts/PassThrough/expert.md
```

Mark all three files as `CopyToOutputDirectory = PreserveNewest` in the `.csproj`:

```xml
<ItemGroup>
  <None Include="Missions/**" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

---

## Acceptance criteria

- `dotnet build` passes with `GitHub.Copilot.SDK` added
- `InMemorySessionStore` stores and retrieves sessions without touching disk
- `OaiServerFixture.StartAsync` binds to a free port and responds to
  `GET /v1/models` returning HTTP 200
- No-op mission files are copied to output directory and can be loaded by
  `MclParser.Parse` + `ExpertLoader.LoadFromLockFile` in a test
