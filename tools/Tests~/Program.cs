using Capisoft.Lib.BaComputerGames;
using UnityEngine;

static class Program
{
    static int passed;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    static async Task Reject<T>(Func<Task> action, string name) where T : Exception
    { try { await action(); } catch (T) { Check(true, name); return; } throw new Exception("Expected " + typeof(T).Name + ": " + name); }
    static ComputerGameDefinition Def(string id, IComputerGameLoader loader = null) => ComputerGameDefinition.Create<FakeGame>(id, id, "test", loader: loader);

    static async Task Main()
    {
        await Reject<ArgumentException>(() => Task.FromResult(Def("Invalid")), "Ids require a namespace");
        await Reject<ArgumentException>(() => Task.FromResult(Def("author:UPPER")), "Ids are lowercase");
        await Reject<ArgumentException>(() => Task.FromResult(Def("a:b:c")), "Multiple namespaces rejected");
        using var first = ComputerGames.Register("alpha-mod", "/alpha", Def("test:alpha"));
        using var second = ComputerGames.Register("beta-mod", "/beta", Def("test:beta"));
        Check(ComputerGames.Catalog.Count == 2 && !ComputerGames.IsHostActive, "Registration works before host loads");
        Check(GameObject.Components == 0, "Registration creates no gameplay components");
        var menu = new ComputerGamesCatalog(); menu.Refresh();
        Check(menu.Count == 3 && menu.Selected.Id == ComputerGames.VanillaBrickBreakerId, "Monitor menu includes vanilla before registered games");
        menu.Move(-1); Check(menu.Selected.Id == "test:beta", "Up from first game wraps to last");
        menu.Move(1); Check(menu.SelectedIndex == 0, "Down from last game wraps to first");
        menu.Move(1);
        using (var inserted = ComputerGames.Register("extra", "", Def("test:aardvark")))
        {
            menu.Refresh(); Check(menu.Selected.Id == "test:alpha", "Registry insert preserves highlighted game by id");
            menu.Move(-1); Check(menu.Selected.Id == "test:aardvark", "New game becomes selectable");
        }
        menu.Refresh(); Check(menu.Selected.Id == ComputerGames.VanillaBrickBreakerId, "Removing selected game returns highlight to vanilla");
        Check(GameObject.Components == 0 && ComputerGames.Sessions.Count == 0, "Menu refresh and navigation never instantiate or prepare games");
        var snapshot = ComputerGames.Catalog;
        Check(ReferenceEquals(snapshot, ComputerGames.Catalog), "Catalog polling does not allocate each frame");
        await Reject<InvalidOperationException>(() => ComputerGames.PrepareAsync("test:alpha"), "Disabled host rejects launch");
        await Reject<InvalidOperationException>(() => Task.FromResult(ComputerGames.Register("other", "/", Def("test:alpha"))), "Duplicate id does not overwrite owner");
        ComputerGames.ActivateHost(); ComputerGames.ActivateHost();
        Check(ComputerGames.Catalog.Count == 2, "Host activation preserves early registrations");
        var session = await ComputerGames.PrepareAsync("test:alpha");
        Check(session.ModRootPath == "/alpha" && session.Context.GameId == "test:alpha", "Session retains owner asset path and game id");
        Check(GameObject.Components == 0 && ComputerGames.Sessions.Count == 1, "Preparation still defers gameplay construction");
        var game = new FakeGame(); session.Instance = game; game.Initialize(session.Context);
        Check(game.Initialized == 1, "Initialize called once");
        await Reject<InvalidOperationException>(() => { game.Initialize(session.Context); return Task.CompletedTask; }, "Double initialization rejected");
        int events = 0, errors = 0; ComputerGameResult result = null;
        Action<Exception> errorHandler = _ => errors++;
        Action<ComputerGameResult> badSubscriber = _ => throw new Exception("subscriber failure");
        Action<ComputerGameResult> subscriber = r => { events++; result = r; };
        ComputerGames.Error += errorHandler; ComputerGames.RoundCompleted += badSubscriber; ComputerGames.RoundCompleted += subscriber;
        Check(!session.Context.CompleteRound(7), "No result before round starts");
        session.Context.BeginRound(); game.Tick(new ComputerGameFrame(0.25f, false, false, default));
        Check(session.Context.CompleteRound(7, 2), "One result emitted on completed round");
        Check(events == 1 && errors == 1, "Failing score subscriber does not block others");
        Check(result.GameId == "test:alpha" && result.GameVersion == "0.1.0" && result.Ruleset == "default-v1" && result.Score == 7 && result.Level == 2, "Result identifies game version and ruleset");
        Check(result.ActiveSeconds == 0.25 && result.ElapsedSeconds >= 0 && result.EndedAtUtc >= result.StartedAtUtc, "Result has active time and UTC timing");
        Check(!session.Context.CompleteRound(100) && events == 1, "Repeated completion emits no duplicate");
        session.Context.BeginRound();
        await Reject<ArgumentOutOfRangeException>(() => Task.FromResult(session.Context.CompleteRound(-1)), "Negative scores rejected");
        session.Dispose(); session.Dispose(); game.Tick(new ComputerGameFrame(1, true, false, default));
        Check(game.Shutdowns == 1 && game.Ticks == 1 && session.IsClosed && ComputerGames.Sessions.Count == 0, "Closing is idempotent and stops tick");
        Check(!session.Context.CompleteRound(8) && events == 1, "Abandoned round emits no score");
        ComputerGames.RoundCompleted -= badSubscriber; ComputerGames.RoundCompleted -= subscriber; ComputerGames.Error -= errorHandler;

        var delayed = new DelayedLoader();
        using var lazy = ComputerGames.Register("lazy-mod", "/lazy", Def("test:lazy", delayed));
        Check(delayed.Loads == 0, "Loader not called at registration");
        using var cancel = new CancellationTokenSource();
        var pending = ComputerGames.PrepareAsync("test:lazy", cancel.Token);
        Check(delayed.Loads == 1 && !pending.IsCompleted, "Loader starts only at preparation");
        cancel.Cancel(); var lateAssets = new FakeAssets(); delayed.Complete(lateAssets);
        await Reject<OperationCanceledException>(() => pending, "Selection cancellation respected by noncooperative loader");
        Check(lateAssets.Disposals == 1 && ComputerGames.Sessions.Count == 0, "Late resources released once after cancellation");
        pending = ComputerGames.PrepareAsync("test:lazy"); var assets = new FakeAssets(); delayed.Complete(assets);
        var loaded = await pending;
        Check(ReferenceEquals(loaded.Context.Assets, assets) && assets.Disposals == 0, "Resources delivered to context");
        loaded.Context.RequestExit(); loaded.Dispose(); Check(assets.Disposals == 1, "Resources disposed once on requested exit");
        pending = ComputerGames.PrepareAsync("test:lazy");
        ComputerGames.DeactivateHost(); ComputerGames.ActivateHost();
        var hostLate = new FakeAssets(); delayed.Complete(hostLate);
        await Reject<OperationCanceledException>(() => pending, "Old host load cannot survive reactivation");
        Check(hostLate.Disposals == 1, "Host cancellation releases late assets");
        pending = ComputerGames.PrepareAsync("test:lazy"); lazy.Dispose(); var unregisterLate = new FakeAssets(); delayed.Complete(unregisterLate);
        await Reject<OperationCanceledException>(() => pending, "Unregister cancels in-flight load");
        Check(unregisterLate.Disposals == 1, "Unregister releases late assets once");
        using var replacement = ComputerGames.Register("new-owner", "/new", Def("test:lazy"));
        lazy.Dispose(); Check(ComputerGames.Catalog.Any(d => d.Id == "test:lazy"), "Old registration cannot remove replacement");
        Check(snapshot.Count == 2, "Previous metadata snapshot stays stable");
        var alpha = await ComputerGames.PrepareAsync("test:alpha");
        var beta = await ComputerGames.PrepareAsync("test:beta"); first.Dispose();
        Check(alpha.IsClosed && !beta.IsClosed, "Unregister only closes owner's sessions");
        ComputerGames.DeactivateHost(); Check(beta.IsClosed && ComputerGames.Sessions.Count == 0, "Host shutdown closes remaining sessions");
        ComputerGames.ActivateHost();
        using var broken = ComputerGames.Register("broken", "/broken", Def("test:broken", new BrokenLoader()));
        await Reject<IOException>(() => ComputerGames.PrepareAsync("test:broken"), "Loader failure surfaces without creating session");
        Check(ComputerGames.Sessions.Count == 0, "No leaked session after loader exception");
        await Reject<OperationCanceledException>(() => ComputerGames.PrepareAsync("test:broken", new CancellationToken(true)), "Already cancelled request does not call loader");
        await Reject<KeyNotFoundException>(() => ComputerGames.PrepareAsync("test:missing"), "Missing game rejected");
        var mod = new ExampleMod();
        Check(mod.RelativeAssetBundlePaths.Length == 0, "Base mod declares no startup bundles");
        await mod.OnLoadAsync(new BAModAPI.ModContext { ModId = "example-mod", ModRootPath = "/example" });
        Check(ComputerGames.Catalog.Any(d => d.Id == "example:game"), "Base mod automatically registers");
        var auto = await ComputerGames.PrepareAsync("example:game");
        await mod.OnUnloadAsync(); await mod.OnUnloadAsync();
        Check(auto.IsClosed && !ComputerGames.Catalog.Any(d => d.Id == "example:game"), "Base mod unregisters and closes game");
        var nativeDisplay = Def("test:native-display");
        var cleanDisplay = nativeDisplay.WithNativeRetroEffects(false);
        Check(nativeDisplay.UseNativeRetroEffects && !cleanDisplay.UseNativeRetroEffects, "Display option returns a new descriptor without mutating defaults");
        int displayBegins = 0; var displayScope = new FakeAssets();
        ComputerGames.BeginDisplaySession = () => { displayBegins++; return displayScope; };
        using var displayRegistration = ComputerGames.Register("display-mod", "/", cleanDisplay);
        var display = await ComputerGames.PrepareAsync("test:native-display");
        display.PrepareDisplay(); display.PrepareDisplay();
        Check(displayBegins == 1, "Native display scope begins only once after screen setup");
        display.Dispose(); display.Dispose();
        Check(displayScope.Disposals == 1, "Display scope is disposed exactly once");
        display.PrepareDisplay();
        Check(displayBegins == 1, "Closed session cannot recreate a display scope");
        var untouched = await ComputerGames.PrepareAsync("test:beta"); untouched.PrepareDisplay(); untouched.Dispose();
        Check(displayBegins == 1, "Games using default display retain native effects");
        ComputerGames.BeginDisplaySession = () => throw new InvalidOperationException("optional display hook unavailable");
        var missingDisplay = await ComputerGames.PrepareAsync("test:native-display");
        missingDisplay.PrepareDisplay();
        Check(!missingDisplay.IsClosed, "Missing optional display hook does not close the game");
        missingDisplay.Dispose(); ComputerGames.BeginDisplaySession = null;
        ComputerGames.DeactivateHost();
        RecordsHarness.Run(Check, Path.Combine(Path.GetTempPath(), "mcg-record-tests-" + Guid.NewGuid().ToString("N")));
        LocalesHarness.Run(Check);
        Console.WriteLine("COMPLETE " + passed + " assertions");
    }
}
public sealed class FakeGame : ComputerGameBehaviour
{
    public int Initialized, Ticks, Shutdowns;
    public override Camera Camera => null;
    protected override void OnInitialize() { Initialized++; }
    protected override void OnTick(ComputerGameFrame frame) { Ticks++; }
    protected override void OnShutdown() { Shutdowns++; }
}
sealed class FakeAssets : ComputerGameAssets { public int Disposals; public override void Dispose() { Disposals++; } }
sealed class DelayedLoader : IComputerGameLoader
{
    public int Loads; TaskCompletionSource<ComputerGameAssets> pending;
    public Task<ComputerGameAssets> LoadAsync(ComputerGameLoadContext context, CancellationToken cancellation) { Loads++; pending = new(); return pending.Task; }
    public void Complete(ComputerGameAssets assets) { pending.SetResult(assets); }
}
sealed class BrokenLoader : IComputerGameLoader
{ public Task<ComputerGameAssets> LoadAsync(ComputerGameLoadContext context, CancellationToken cancellation) => throw new IOException("deliberate failure"); }
sealed class ExampleMod : ComputerGameMod<FakeGame>
{ protected override ComputerGameDefinition Definition => ComputerGameDefinition.Create<FakeGame>("example:game", "Example", ""); }
