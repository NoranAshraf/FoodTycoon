# FoodTycoon

Mobile idle game in Unity 6 (6000.6.0f1, URP, new Input System only) that clones the core loop of *Idle Food Tycoon:
Food Clicker*: grinders beside a conveyor belt drop product pieces, the belt carries them to a packing machine, boxes
stack on the counter, get loaded onto a truck and are sold. **Scope is the core loop + upgrades only — no ads, no
meta game.** Everything lives in the single scene `Assets/Scenes/SampleScene.unity`.

## Project layout

- `Assets/_Game/Scripts/` — all game code (one class per file, no namespaces).
- `Assets/_Game/Prefabs/` — `Grinder`, `ProductCube` (belt piece), `PackagedBox`, `Truck`.
- `Assets/_Game/Upgrades/` — one `UpgradeDefinition` asset per upgrades-menu row.
- `Assets/_Game/UI/` — `HUD.uxml`, `HUD.uss`, `HUDPanelSettings.asset` (1080×2400 reference, match width),
  `UnityDefaultRuntimeTheme.tss`; `UpgradesPanel.uxml/.uss` (overlay, instanced inside `HUD.uxml`),
  `UpgradeRow.uxml` (row template), `UpgradeIcons/*.uxml` (one drawn icon per upgrade).
- `Assets/TestAssets/` — third-party art (grinder, packing machine, truck, materials). Don't edit these; wrap them in
  our own prefabs instead.
- `Assets/Materials/` — our own materials (`Product`, `Road`, `Ground`).

## Game systems (how the loop is wired)

| Script | Role |
|---|---|
| `GrinderLine` | Owns the 4 machine `Slot`s, `TryBuyMachine()` / `TryMerge()`, price curves, `OnChanged`. Starts with `initialGrinder` in slot 1. |
| `Grinder` | Per-cycle scatters `CurrentPiecesPerCycle` pieces onto the belt at random spots/timings. `Level` doubles piece count per level (`levelOutputMultiplier`), tints `accentRenderers`, shows a `TextMesh` level label. `Dismantle()` when merged away. Pools its pieces (`ObjectPool<BeltItem>`). |
| `ConveyorBelt` | Scrolls the belt texture (`_BaseMap_ST` via MaterialPropertyBlock) and moves `BeltItem`s from `pathStart` to `pathEnd`; raises `OnItemReachedEnd`. |
| `PackingMachine` | Consumes `piecesPerBox` pieces (summing their `Value`) into a `PackagedBox`, which flies onto the counter `BoxStack`. Owns the box pool (`ObjectPool<PackagedBox>`); never `Destroy` a box, call `PackagedBox.ReturnToPool()`. |
| `BoxStack` | Grid stack with ticket-based reservations so in-flight boxes target the right slot; counter is unlimited, truck bed holds 16. |
| `BoxDispatcher` | Moves the oldest counter box (after `dwellTime`) to `Destination` (the docked truck bed). |
| `Truck` / `TruckDepot` | Depot keeps one truck docked; `Sell()` pays `LoadValue` into the `Wallet`, drives it out right and a new truck in from the left, redirecting the dispatcher. A departing truck releases its bed boxes to the pool (`BoxStack.ReleaseAll()`) before despawning. |
| `Wallet` | `double` balance, `Add` / `TrySpend`, `OnBalanceChanged`. |
| `UpgradeStat` / `UpgradeDefinition` | Enum of scalable values (`ConveyorSpeed`, `GrindingSpeed`, `GrinderMoney`, `LoadingSpeed`) and the ScriptableObject describing one menu row: title, icon UXML, stat, `maxLevel`, `bonusPerLevel` (`×(1 + bonus·level)`), `basePrice × priceGrowth^level`. |
| `UpgradeManager` | On `GameManager`. Tracks levels, `TryBuy()` spends `Wallet` money, `GetMultiplier(stat)` folds every definition for that stat, `OnChanged`. Lazy-initialised so readers may subscribe in `OnEnable`. |
| `HudController` | Binds `HUD.uxml` (money pill, SELL, ADD MACHINE, MERGE, UPGRADES tab) to `Wallet`, `TruckDepot`, `GrinderLine`, `UpgradesPanelController`. |
| `UpgradesPanelController` / `UpgradeRowView` | On `HUD`. Builds one `UpgradeRow.uxml` per definition into the `upgrades-rows` ScrollView, refreshes on `UpgradeManager.OnChanged` + wallet changes, `Show()`/`Hide()` (X button or overlay tap). |
| `MoneyFormatter` | `$7`, `$42.0`, `$101.7k` … (`wholeDollars` for prices/sell amount). |

Economy defaults (all Inspector-tunable): piece `$3.5`, 2 pieces per box → `$7` a box; machines `$30 × 1.6ⁿ`,
merges `$60 × 2.2^(level−1)`, rounded to whole dollars. Merge takes the lowest pair of equal-level machines and keeps
the earlier slot; machines cap at level 2 (`GrinderLine.maxMachineLevel`), so two level-2s never count as a pair. Upgrades (same wallet money, 10 levels each): conveyor `$20 × 1.5ⁿ` +20%/lv, grinding speed
`$40 × 1.6ⁿ` +20%/lv, grinder money `$50 × 1.7ⁿ` +25%/lv, loading speed `$25 × 1.5ⁿ` +20%/lv.

**Adding an upgrade:** add a member to `UpgradeStat` (if it is a new kind of effect), create an `UpgradeDefinition`
asset (Create → FoodTycoon → Upgrade) with an icon UXML under `UI/UpgradeIcons/`, add it to `UpgradeManager.upgrades`
on `GameManager`, and have the consuming system hold a serialized `UpgradeManager` reference, read
`GetMultiplier(stat)` in `OnEnable` and again on `OnChanged` (see `ConveyorBelt`, `GrinderLine`, `BoxDispatcher`).
Speed stats divide a time (`cycleTime / multiplier`); money stats multiply a value.

## Code conventions

- Follow the `unity-csharp` skill: Allman braces, 4 spaces, 120 cols, `#region` order Serialized Fields → Private
  Fields → Public Properties → Events → Lifecycle → Public Methods → Private Methods.
- `[SerializeField] private camelCase` with a `[Tooltip]` on every field; no public fields. Booleans `is/has/can`,
  coroutines `…Routine`, events `On…`.
- Cross-system communication goes through C# events (`OnChanged`, `OnBalanceChanged`, …) or direct serialized
  references wired in the scene — never `Find…` at runtime, never `SendMessage`.
- Avoid per-frame allocations (no LINQ / `new WaitForSeconds` in hot paths; loop with `yield return null`).
- Anything spawned repeatedly (belt pieces, boxes) goes through `ObjectPool<T>`; pooled objects are instantiated
  at the **scene root** (no container/parent object) and re-parented back to the root on release.
- Money is `double`; per-piece values are `float`.

## UI

- **Always implement UI with Unity UI Toolkit: UXML for layout, USS for styling, `UIDocument` for runtime screens.**
- Do not use uGUI (`Canvas`, `Image`, `Button`, `TextMeshProUGUI`) or IMGUI (`OnGUI`) for game UI, even for quick
  prototypes or single elements. (World-space labels on 3D objects, like the truck `0/16` and machine `LV n`, use the
  built-in `TextMesh` and billboard to `Camera.main` in `LateUpdate`.)
- Keep UXML/USS assets under `Assets/_Game/UI/`, one UXML per screen/panel, shared styles in a common USS. Design
  against the 1080×2400 portrait reference; icons are drawn with plain styled elements, no sprites.
- Drive UI from C# by querying elements (`rootVisualElement.Q<...>`) and binding to game state; keep game logic out of
  UI scripts. Disable buttons with `SetEnabled(false)` and style `:disabled` explicitly.

## Working with the Editor (Unity CLI / `com.unity.pipeline`)

The user keeps the Editor open on SampleScene — drive it with `unity command …`; never hand-edit `.unity` /
`.prefab` / `.asset` YAML while it is running.

- `unity command eval_file --file x.cs`: script body only — **no `using` directives** (fully-qualify everything),
  float literals need `f`, use `FindAnyObjectByType` (not `FindFirstObjectByType`), UQuery via
  `UnityEngine.UIElements.UQueryExtensions.Q<T>(root, "name")`. Unity "fake null" breaks `??` — use explicit
  `if (x == null)` checks.
- After writing scripts call `UnityEditor.AssetDatabase.Refresh()` from eval, then poll `recompile_status`.
- Wire scene references through `SerializedObject` / `Undo` helpers and finish with `EditorSceneManager.SaveScene`;
  leave the scene saved and not dirty.
- Play-mode tests: `editor_play`, then set `Application.runInBackground = true` from eval (the Editor is unfocused and
  time would freeze). Simulate UI Toolkit clicks with `PointerDownEvent`/`PointerUpEvent` from an IMGUI `Event`
  (`ClickEvent` alone doesn't fire `Button.clicked`). Always `editor_stop` when done.
- Screenshots: `capture_game_view --source screen --width 540 --height 1200 --save_path Temp/claude/x.png` lands in
  `Assets/Temp/claude/`; delete `Assets/Temp` afterwards (`AssetDatabase.DeleteAsset("Assets/Temp")`). Game view is
  the custom "Portrait 9:20" (1080×2400) size.
- Never trigger modal Editor dialogs from eval (e.g. `AssetDatabase.ImportPackage`) — they block the main thread and
  time out; ask the user to do those imports in the Editor.
- GPU Resident Drawer is disabled on both URP assets on purpose (it culled the imported meshes in the Game view).
