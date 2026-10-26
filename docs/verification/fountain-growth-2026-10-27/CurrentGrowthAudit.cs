// Read-only product audit. Writes only to a new scratch save directory supplied
// as argument 0; never reads or changes a player's save or product values.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Mofumachi.Core;
using Mofumachi.Presentation;

internal static class CurrentGrowthAudit
{
    static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    static string Number(float x) => x.ToString("R", CultureInfo.InvariantCulture);
    static string SceneryKey(TownSceneryPlacement p) =>
        $"scenery:{p.Kind}:{Number(p.Feet.X)}:{Number(p.Feet.Y)}:{p.Level}:{Number(p.Width)}:{Number(p.Height)}";
    static string GrowthKey(TownGrowthPlacement p) =>
        $"growth:{p.Prop}:{Number(p.Position.X)}:{Number(p.Position.Y)}:{p.Level}:{Number(p.Width)}";
    static readonly TownDistrictId[] Districts = {
        TownDistrictId.Central, TownDistrictId.West, TownDistrictId.East,
        TownDistrictId.North, TownDistrictId.South };
    static Dictionary<string, string[]> Decor(int level)
    {
        // Mirrors TownSceneryView's composition/level predicates and the live
        // TownGrowthView's props. This is logical eligibility, not a rendered
        // viewport, occlusion, screenshot, or Pixel result.
        int stage = Math.Max(0, Math.Min(6, level));
        var scenery = TownSceneryLayout.Landmarks.Where(p => p.Kind != TownSceneryKind.Fountain)
            .Concat(TownSceneryLayout.Nature).Concat(TownVillageDressing.All)
            .Concat(TownCentralMasterLayout.Dressing).Concat(TownWestMasterLayout.Dressing)
            .Concat(TownEastMasterLayout.Dressing).Concat(TownNorthMasterLayout.Dressing)
            .Concat(TownSouthMasterLayout.Dressing)
            .Where(p => TownCentralMasterLayout.VisibleAtLevel(p, stage)
                && TownWestMasterLayout.VisibleAtLevel(p, stage)
                && TownEastMasterLayout.VisibleAtLevel(p, stage)
                && TownNorthMasterLayout.VisibleAtLevel(p, stage)
                && TownSouthMasterLayout.VisibleAtLevel(p, stage)).ToArray();
        return Districts.ToDictionary(d => d.ToString(), d => scenery.Where(p => p.District == d)
            .Select(SceneryKey).Concat(TownGrowthPresentation.ForDistrict(d, stage).Select(GrowthKey))
            .OrderBy(s => s, StringComparer.Ordinal).ToArray());
    }
    static bool SameDecor(Dictionary<string, string[]> a, Dictionary<string, string[]> b)
        => Districts.All(d => a[d.ToString()].SequenceEqual(b[d.ToString()]));
    public static int Main(string[] args)
    {
        Require(args.Length == 1, "Supply one new scratch directory.");
        string root = Path.GetFullPath(args[0]);
        Require(!Directory.Exists(root), "Refuse to overwrite an existing directory or save.");
        Directory.CreateDirectory(root);
        var store = new SaveService(Path.Combine(root, "scratch-save"));
        var initial = store.Load();
        Require(!initial.Recovered && initial.State.townGrowthLevel == 0
            && initial.State.completedQuestIds.Count == 0 && initial.State.coins == 0
            && initial.State.mergeBoard.Count == 2 && initial.State.favoriteCharacterId == "",
            "Fresh state differs from current authored initial state.");
        var game = new GameStateManager(initial.State, store);
        Require(game.SelectFavorite("noah"), "Real favorite selection save failed.");
        var transitions = new List<object>();
        int expectedCoins = 0;
        foreach (var expected in QuestCatalog.All)
        {
            var quest = QuestCatalog.NextFor(game.State);
            Require(quest != null && quest.Id == expected.Id, "NextFor differs from authored UI order.");
            int previous = game.State.townGrowthLevel, generated = 0, merges = 0;
            var before = Decor(previous);
            Require(game.Quests.AcceptQuest(quest), "Quest acceptance failed: " + quest.Id);
            foreach (var requirement in quest.Requirements)
            {
                int guard = 0;
                while (game.State.mergeBoard.Count(i => i.itemId == requirement.ItemId
                    && i.level == requirement.Level) < requirement.Count)
                {
                    Require(++guard < 100, "Production did not progress.");
                    var pair = game.State.mergeBoard.Where(i => i.itemId == requirement.ItemId
                        && i.level < requirement.Level).GroupBy(i => i.level).FirstOrDefault(g => g.Count() >= 2);
                    if (pair == null)
                    {
                        Require(game.State.productionFrameIds.Contains(requirement.ItemId), "Missing real production frame.");
                        Require(game.AddItem(requirement.ItemId, 1), "Real level-one generation failed.");
                        generated++;
                    }
                    else
                    {
                        var items = pair.Take(2).ToArray();
                        Require(game.TryMerge(items[0].cellIndex, items[1].cellIndex), "Real merge failed.");
                        // In Unity the existing merge animation releases these locks.
                        game.Board.ReleaseLocks(); merges++;
                    }
                }
            }
            Require(game.Quests.TryDeliver().Success, "Real delivery failed.");
            expectedCoins += quest.Coins;
            Require(game.State.townGrowthLevel == previous + 1 && game.State.coins == expectedCoins,
                "Town level or coins differ from transaction result.");
            var after = Decor(game.State.townGrowthLevel);
            string saved = StateCodec.Encode(game.State);
            var loaded = new SaveService(Path.Combine(root, "scratch-save")).Load();
            Require(!loaded.Recovered && StateCodec.Encode(loaded.State) == saved,
                "Fresh disk load did not preserve the complete state.");
            game = new GameStateManager(loaded.State, new SaveService(Path.Combine(root, "scratch-save")));
            Require(SameDecor(after, Decor(game.State.townGrowthLevel)), "Five-district decor changed after load.");
            Require(!game.Quests.TryDeliver().Success && !game.Quests.AcceptQuest(quest)
                && StateCodec.Encode(game.State) == saved, "Completed quest replay changed saved rewards/state.");
            File.Copy(store.PrimaryPath, Path.Combine(root, "level-" + game.State.townGrowthLevel + "-save.json"));
            transitions.Add(new {
                from = previous, to = game.State.townGrowthLevel, quest_id = quest.Id,
                quest_title = quest.Title, requester = quest.RequesterId,
                requirements = quest.Requirements.Select(r => new { item_id=r.ItemId, item_level=r.Level, count=r.Count }),
                additional_completed_quests = 1, cumulative_completed_quests=game.State.completedQuestIds.Count,
                coins = quest.Coins, cumulative_coins = game.State.coins,
                level_one_items_generated = generated, merges,
                level_one_equivalent = quest.Requirements.Sum(r => r.Count * (1 << (r.Level - 1))),
                save_full_state_equal_after_fresh_load = true, replay_rejected_without_state_change = true,
                districts = Districts.ToDictionary(d => d.ToString(), d => new {
                    added = after[d.ToString()].Except(before[d.ToString()]).ToArray(),
                    removed_or_replaced = before[d.ToString()].Except(after[d.ToString()]).ToArray(),
                    eligible_before = before[d.ToString()].Length, eligible_after = after[d.ToString()].Length,
                    identical_after_fresh_load = true })
            });
        }
        Require(QuestCatalog.NextFor(game.State) == null && game.State.townGrowthLevel == 6
            && game.State.coins == 330, "Catalog ending differs from current source.");
        var report = new {
            kind = "current-core-delivery-disk-load-and-logical-decor-audit",
            product_values_changed = false, unity_run = false, pixel_run = false,
            app_process_restart_tested = false, performance_tested = false,
            timing_note = "CPU audit runtime is not human play time or device FPS.",
            initial_internal_and_hud_level = 0, initial_free_tea_level_one_items = 2,
            initial_favorite_selection = "noah", production = "level-one only, real AddItem/TryMerge APIs",
            xp_thresholds = (int[])null, town_rule = "max(saved town level, unique completed main quest count)",
            transitions, next_main_quest_after_six = (string)null,
            logical_decor_at_seven_equals_six = SameDecor(Decor(6), Decor(7)),
            caveat = "Logical stage eligibility excludes viewport culling, artwork contents, occlusion, growth UI timing, Android lifecycle, and Pixel performance."
        };
        File.WriteAllText(Path.Combine(root, "result.json"), JsonSerializer.Serialize(report,
            new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Verified six real core deliveries, six full disk reloads, all five district eligibility snapshots, reward replay rejection. Final Lv6 /330 coins. Unity/Pixel not executed.");
        return 0;
    }
}
