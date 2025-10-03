using System;
using Game.Application.Exploration;
using Game.Application.Battle;
using Game.Application.Menu;
using Game.Application.Save;
using Game.Infrastructure.Persistence;
using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Domain;
using Game.Core.Domain.Items;
using Game.Infrastructure.ConsoleUI;
using Game.Infrastructure.Data;

// GLITCH QUEST: Minimal playable loop (exploration + interactions)
Console.Title = "Glitch Quest — Les Ruines du Terminal";
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.CursorVisible = false;

// Services and infrastructure
IRenderer renderer = new ConsoleRenderer();
IInput input = new ConsoleInput();
IRandom rng = new DefaultRandom();
var exploration = new ExplorationService(rng);
var interaction = new ExplorationInteractionService();
var data = new FileDataLoader(AppContext.BaseDirectory);

// Save system (slot-based JSON)
var savesDir = System.IO.Path.Combine(AppContext.BaseDirectory, "saves");
var saveRepo = new JsonSaveRepository(savesDir);
var saveService = new SaveService(saveRepo);

// Load type chart (for battles)
var typeChart = data.LoadTypeChart();
var battleService = new BattleService(rng, typeChart);
var menuService = new MenuService();

// Initialize after Title Menu selection
string currentMapId = "parc";
Map map = new Map(new Tile[1,1] { { new Tile(TileType.Floor, '.', true) } }, (0,0));
ExplorationState state = new ExplorationState(map);
PlayerCharacter player = new PlayerCharacter("Hero", 1, Element.Normal, new Stats(10, 5, 1, 1, 1, 1.0, 1.0), Archetype.Balanced);
Inventory inventory = new Inventory();

int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);

void SafeSetCursorPosition(int left, int top)
{
    try
    {
        int maxLeft = Math.Max(0, Console.BufferWidth - 1);
        int maxTop = Math.Max(0, Console.BufferHeight - 1);
        int safeLeft = Clamp(left, 0, maxLeft);
        int safeTop = Clamp(top, 0, maxTop);
        Console.SetCursorPosition(safeLeft, safeTop);
    }
    catch
    {
        // As an ultimate fallback (rare: when console I/O not available), ignore positioning.
    }
}

void DrawHud()
{
    SafeSetCursorPosition(0, map.Height + 1);
    Console.WriteLine("[Flèches] Se déplacer  |  [Entrée] Interagir  |  [M] Menu  |  [Échap] Quitter   ");
}

void Render()
{
    renderer.Clear();
    renderer.DrawMap(map, state.PlayerX, state.PlayerY);
    DrawHud();
    renderer.Present();
}

void LoadMapAndRespawn(string mapId)
{
    var lines = data.ReadMapLines(mapId);
    map = exploration.LoadMapFromAscii(lines);
    // Attach simple encounter tables per map (data-driven wiring)
    map.Encounters = BuildEncounterTableFor(mapId);
    state = new ExplorationState(map);
    currentMapId = mapId;
    Render();
}

EnemyCharacter? _currentEnemy = null; // used only for status rendering in Choose()

// Helper: encounter tables per map (simple built-in tables; can be moved to data later)
EncounterTable BuildEncounterTableFor(string mapId)
{
    // Zone-specific tables with moderated levels and mixed types to avoid one-shots
    if (string.Equals(mapId, "parc", StringComparison.OrdinalIgnoreCase))
    {
        return new EncounterTable(new[]
        {
            new EncounterEntry("sprout", 1, 2, 50),
            new EncounterEntry("bugling", 2, 3, 30),
            new EncounterEntry("gelblob", 2, 3, 20)
        });
    }
    if (string.Equals(mapId, "labo", StringComparison.OrdinalIgnoreCase))
    {
        return new EncounterTable(new[]
        {
            new EncounterEntry("bugling", 3, 4, 30),
            new EncounterEntry("emberling", 3, 4, 40),
            new EncounterEntry("gelblob", 3, 4, 30)
        });
    }
    if (string.Equals(mapId, "noyau", StringComparison.OrdinalIgnoreCase))
    {
        return new EncounterTable(new[]
        {
            new EncounterEntry("vinebeast", 4, 6, 35),
            new EncounterEntry("emberling", 5, 6, 35),
            new EncounterEntry("bugling", 5, 6, 30)
        });
    }
    return new EncounterTable(new[] { new EncounterEntry("sprout", 1, 2, 100) });
}

// === Battle UI helpers ===
MenuOption? Choose(string title, IReadOnlyList<MenuOption> options, int startIndex = 0)
{
    if (options == null || options.Count == 0) return null;
    // Ensure we start on an enabled option
    int idx = Math.Clamp(startIndex, 0, options.Count - 1);
    idx = NextEnabledIndex(options, idx);
    while (true)
    {
        Console.Clear();
        Console.WriteLine("===============================");
        Console.WriteLine("           COMBAT");
        Console.WriteLine("===============================");
        renderer.DrawBattleStatus(player, _currentEnemy!);
        Console.WriteLine();
        Console.WriteLine(title);
        for (int i = 0; i < options.Count; i++)
        {
            var o = options[i];
            var prefix = i == idx ? "> " : "  ";
            var label = o.Label + (o.Enabled ? "" : " (indisponible)");
            Console.WriteLine(prefix + label);
        }
        var k = input.ReadKey();
        if (k == InputKey.Up) idx = PrevEnabledIndex(options, idx);
        else if (k == InputKey.Down) idx = NextEnabledIndex(options, idx);
        else if (k == InputKey.Enter && options[idx].Enabled) return options[idx];
        else if (k == InputKey.Escape) return null;
    }

    static int NextEnabledIndex(IReadOnlyList<MenuOption> opts, int from)
    {
        int i = from;
        for (int n = 0; n < opts.Count; n++)
        {
            i = (i + 1) % opts.Count;
            if (opts[i].Enabled) return i;
        }
        return from;
    }
    static int PrevEnabledIndex(IReadOnlyList<MenuOption> opts, int from)
    {
        int i = from;
        for (int n = 0; n < opts.Count; n++)
        {
            i = (i - 1 + opts.Count) % opts.Count;
            if (opts[i].Enabled) return i;
        }
        return from;
    }
}


EnemyCharacter CreateEnemyFromId(string enemyId, int level, Dictionary<string, Game.Infrastructure.Data.FileDataLoader.EnemyTemplate> enemyCatalog, Dictionary<string, Move> moveCatalog)
{
    if (enemyCatalog.TryGetValue(enemyId, out var tmpl))
    {
        var e = new EnemyCharacter(tmpl.Name, level, tmpl.Type, tmpl.BaseStats, new SimpleAiStrategy());
        foreach (var mvId in tmpl.MoveIds)
        {
            if (moveCatalog.TryGetValue(mvId, out var mv)) e.Moves.Add(mv);
        }
        // Fallback if no moves mapped
        if (e.Moves.Count == 0)
        {
            e.Moves.Add(new Move { Id = "spark", Name = "Étincelle", Type = Element.Electric, Power = 45, Kind = DamageKind.Physical, Accuracy = 0.90, MpCost = 4, CritChance = 0.10 });
        }
        return e;
    }
    // Fallback hardcoded enemy
    var def = new EnemyCharacter("Bugling", Math.Max(1, level), Element.Electric, new Stats(24, 8, 6, 4, 5, 1.0, 1.0), new SimpleAiStrategy());
    def.Moves.Add(new Move { Id = "spark", Name = "Étincelle", Type = Element.Electric, Power = 45, Kind = DamageKind.Physical, Accuracy = 0.90, MpCost = 4, CritChance = 0.10 });
    return def;
}

bool RunBattleWith(string enemyId, int level, Dictionary<string, Game.Infrastructure.Data.FileDataLoader.EnemyTemplate> enemyCatalog, Dictionary<string, Move> moveCatalog)
{
    _currentEnemy = CreateEnemyFromId(enemyId, level, enemyCatalog, moveCatalog);

    // Battle loop
    while (player.Current.Hp > 0 && _currentEnemy.Current.Hp > 0)
    {
        // Build menus
        var root = menuService.BuildRootBattleMenu(player, inventory);
        var choice = Choose("Choisissez une action (↑/↓ puis Entrée):", root);
        if (choice is null)
        {
            // Treat cancel as attempting to flee
            var fleeIntent = menuService.CreateIntentForFlee(player);
            var enemyIntentCancel = _currentEnemy.ChooseAction(new BattleContext(player, _currentEnemy, rng, typeChart));
            var trCancel = battleService.ResolveTurn(fleeIntent, enemyIntentCancel);
            Console.Clear();
            renderer.DrawBattleStatus(player, _currentEnemy);
            if (trCancel.FirstAction?.Messages is { } m1) { foreach (var m in m1) Console.WriteLine($"> {m}"); }
            if (trCancel.SecondAction?.Messages is { } m2) { foreach (var m in m2) Console.WriteLine($"> {m}"); }
            foreach (var m in trCancel.EndOfTurnMessages) Console.WriteLine($"> {m}");
            renderer.Present();
            input.ReadKey();
            if (trCancel.FirstAction?.Fled == true) { _currentEnemy = null; return true; }
            continue;
        }

        ActionIntent playerIntent;
        if (Equals(choice.Tag, BattleRootChoice.Fuite))
        {
            playerIntent = menuService.CreateIntentForFlee(player);
        }
        else if (Equals(choice.Tag, BattleRootChoice.Objet))
        {
            var itemMenu = menuService.BuildItemSubmenu(inventory, player);
            var itemOpt = Choose("Objet: choisissez un objet", itemMenu);
            if (itemOpt is null) continue; // back
            var item = (Item)itemOpt.Tag!;
            playerIntent = menuService.CreateIntentForItem(player, player, item);
            // note: defer removal until after turn resolution; remove only if actually used
        }
        else
        {
            // Attack or Magic -> choose a move
            var sub = Equals(choice.Tag, BattleRootChoice.Magie)
                ? menuService.BuildMagicSubmenu(player)
                : menuService.BuildAttackSubmenu(player);
            var moveOpt = Choose("Choisissez une compétence", sub);
            if (moveOpt is null) continue; // back
            var move = (Move)moveOpt.Tag!;
            playerIntent = menuService.CreateIntentForMove(player, _currentEnemy, move);
        }

        // Enemy action via AI
        var ctx = new BattleContext(player, _currentEnemy, rng, typeChart);
        var enemyIntent = _currentEnemy.ChooseAction(ctx);

        // Resolve turn
        var tr = battleService.ResolveTurn(playerIntent, enemyIntent);

        // Render turn outcome
        Console.Clear();
        renderer.DrawBattleStatus(player, _currentEnemy);
        if (tr.FirstAction?.Messages is { } fm1) { foreach (var m in fm1) Console.WriteLine($"> {m}"); }
        if (tr.SecondAction?.Messages is { } fm2) { foreach (var m in fm2) Console.WriteLine($"> {m}"); }
        foreach (var m in tr.EndOfTurnMessages) Console.WriteLine($"> {m}");
        renderer.Present();
        input.ReadKey();

        // Consume item only if player's action actually used it this turn
        if (tr.FirstAction?.Actor == player && tr.FirstAction.UsedItem is Item used1) inventory.Remove(used1);
        if (tr.SecondAction?.Actor == player && tr.SecondAction.UsedItem is Item used2) inventory.Remove(used2);

        // Check flee/victory/defeat
        if (tr.FirstAction?.Fled == true)
        {
            _currentEnemy = null; return true; // fled successfully
        }
        if (_currentEnemy.Current.Hp <= 0)
        {
            Console.Clear();
            renderer.DrawBattleStatus(player, _currentEnemy);
            Console.WriteLine("> Victoire !");
            renderer.Present();
            input.ReadKey();
            _currentEnemy = null; return true;
        }
        if (player.Current.Hp <= 0)
        {
            Console.Clear();
            renderer.DrawBattleStatus(player, _currentEnemy);
            Console.WriteLine("> Vous êtes vaincu…");
            renderer.Present();
            input.ReadKey();
            _currentEnemy = null; return false;
        }
    }

    var survived = player.Current.Hp > 0;
    _currentEnemy = null;
    return survived;
}

// Title banner
Console.WriteLine("===============================");
Console.WriteLine(" GLITCH QUEST : Les Ruines du Terminal");
Console.WriteLine("===============================");
Console.WriteLine();

// Load catalogs for potential reconstruction on load
var moveCatalog = data.LoadMoves();
var enemyCatalog = data.LoadEnemies();

// Simple Title Menu: New / Load (slots) / Quit
string FormatSlotPreview(string slotId)
{
    var s = saveService.Load(slotId);
    if (s is null) return $"Charger ({slotId})";
    var time = s.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    return $"Charger ({slotId}) — {s.MapId} @ ({s.PlayerX},{s.PlayerY}) — {time}";
}

MenuOption? TitleMenu()
{
    var slots = saveService.ListSlots().ToHashSet(StringComparer.OrdinalIgnoreCase);
    bool s1 = slots.Contains("slot1");
    bool s2 = slots.Contains("slot2");
    bool s3 = slots.Contains("slot3");
    var opts = new List<MenuOption>
    {
        new("Nouvelle Partie", true, "new"),
        new(FormatSlotPreview("slot1"), s1, "load1"),
        new(FormatSlotPreview("slot2"), s2, "load2"),
        new(FormatSlotPreview("slot3"), s3, "load3"),
        new("Quitter", true, "quit")
    };
    return ChooseSimple("↑/↓ puis Entrée", opts);
}

void StartNewGame()
{
    // Map and exploration state
    currentMapId = "parc";
    try
    {
        var lines = data.ReadMapLines(currentMapId);
        map = exploration.LoadMapFromAscii(lines);
      map.Encounters = BuildEncounterTableFor(currentMapId);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erreur de chargement de la carte: {ex.Message}");
        Console.WriteLine("Assurez-vous que data/maps/parc.txt existe.");
        Environment.Exit(1);
    }
    state = new ExplorationState(map);

    // Player and inventory
    player = new PlayerCharacter(
        name: "Hero",
        level: 1,
        type: Element.Fire,
        baseStats: new Stats(40, 12, 6, 7, 6, 1.0, 1.0),
        archetype: Archetype.Balanced
    );
    player.Moves.Clear();
    if (moveCatalog.TryGetValue("tackle", out var m1)) player.Moves.Add(m1);
    else player.Moves.Add(new Move { Id = "tackle", Name = "Tacle", Type = Element.Normal, Power = 35, Kind = DamageKind.Physical, Accuracy = 0.98, MpCost = 0, CritChance = 0.05 });
    if (moveCatalog.TryGetValue("ember", out var m2)) player.Moves.Add(m2);
    else player.Moves.Add(new Move { Id = "ember", Name = "Flammèche", Type = Element.Fire, Power = 40, Kind = DamageKind.Magic, Accuracy = 0.95, MpCost = 3, CritChance = 0.10 });

    inventory = new Inventory();
}

bool TryLoadFromSlot(string slotId)
{
    var save = saveService.Load(slotId);
    if (save is null) return false;

    // Map/state
    currentMapId = save.MapId;
    var lines = data.ReadMapLines(currentMapId);
    map = exploration.LoadMapFromAscii(lines);
    map.Encounters = BuildEncounterTableFor(currentMapId);
    state = new ExplorationState(map, save.PlayerX, save.PlayerY);

    // Player
    var archetype = Enum.TryParse<Archetype>(save.Player.Archetype, true, out var a) ? a : Archetype.Balanced;
    player = new PlayerCharacter(save.Player.Name, save.Player.Level, save.Player.Type, save.Player.BaseStats, archetype);
    // restore current HP/MP exactly (approx via public API)
    // Set HP to saved value by taking damage from full
    var hpLoss = Math.Max(0, player.BaseStats.Hp - save.Player.Current.Hp);
    if (hpLoss > 0) player.TakeDamage(hpLoss);
    // Set MP to saved value by consuming MP from full
    var mpLoss = Math.Max(0, player.BaseStats.Mp - save.Player.Current.Mp);
    if (mpLoss > 0) player.UseMp(mpLoss);
    player.Moves.Clear();
    foreach (var id in save.Player.MoveIds)
    {
        if (moveCatalog.TryGetValue(id, out var mv))
            player.Moves.Add(mv);
    }
    if (player.Moves.Count == 0)
    {
        // fallback
        if (moveCatalog.TryGetValue("tackle", out var fm)) player.Moves.Add(fm);
    }

    // Inventory
    inventory = new Inventory();
    foreach (var itemId in save.InventoryItemIds)
    {
        if (itemId.StartsWith("potion_hp_", StringComparison.OrdinalIgnoreCase) && int.TryParse(itemId.Substring("potion_hp_".Length), out var hpAmt))
            inventory.Add(new PotionHP(hpAmt));
        else if (itemId.StartsWith("potion_mp_", StringComparison.OrdinalIgnoreCase) && int.TryParse(itemId.Substring("potion_mp_".Length), out var mpAmt))
            inventory.Add(new PotionMP(mpAmt));
        // Other items could be mapped here as needed
    }
    foreach (var keyId in save.Keys)
    {
        inventory.Add(new KeyItem(keyId, $"Clé {keyId}"));
    }

    return true;
}

// Show title and act
while (true)
{
    var sel = TitleMenu();
    if (sel is null) { Console.Clear(); StartNewGame(); break; }
    if (Equals(sel.Tag, "new")) { Console.Clear(); StartNewGame(); break; }
    if (Equals(sel.Tag, "load1"))
    {
        if (TryLoadFromSlot("slot1")) { Console.Clear(); break; }
        Console.WriteLine("Aucune sauvegarde sur slot1.");
        Console.WriteLine("Appuyez sur une touche.");
        input.ReadKey();
    }
    else if (Equals(sel.Tag, "load2"))
    {
        if (TryLoadFromSlot("slot2")) { Console.Clear(); break; }
        Console.WriteLine("Aucune sauvegarde sur slot2.");
        Console.WriteLine("Appuyez sur une touche.");
        input.ReadKey();
    }
    else if (Equals(sel.Tag, "load3"))
    {
        if (TryLoadFromSlot("slot3")) { Console.Clear(); break; }
        Console.WriteLine("Aucune sauvegarde sur slot3.");
        Console.WriteLine("Appuyez sur une touche.");
        input.ReadKey();
    }
    else if (Equals(sel.Tag, "quit"))
    {
        return;
    }
}

Render();

// Helper: build a save snapshot
Game.Core.Domain.SaveGame BuildSave()
{
    var moveIds = player.Moves.Select(m => m.Id).ToList();
    var savePlayer = new SavePlayer(
        Name: player.Name,
        Level: player.Level,
        Type: player.Type,
        BaseStats: player.BaseStats,
        Current: player.Current,
        MoveIds: moveIds,
        Archetype: player.Archetype.ToString()
    );

    var keys = inventory.Items.OfType<KeyItem>().Select(k => k.KeyId).ToList();
    var invIds = inventory.Items.Where(i => i is not KeyItem).Select(i => i.Id).ToList();

    // RNG seed unavailable from DefaultRandom; store a timestamp-based surrogate
    var seed = (int)(DateTime.UtcNow.Ticks % int.MaxValue);

    return new SaveGame(
        MapId: currentMapId,
        PlayerX: state.PlayerX,
        PlayerY: state.PlayerY,
        Player: savePlayer,
        InventoryItemIds: invIds,
        Keys: keys,
        RngSeed: seed,
        SavedAtUtc: DateTime.UtcNow
    ) { SlotId = "slot1" };
}

// Helper: simple vertical menu (for Player Menu)
MenuOption? ChooseSimple(string title, IReadOnlyList<MenuOption> options, int startIndex = 0)
{
    if (options == null || options.Count == 0) return null;
    int idx = Math.Clamp(startIndex, 0, options.Count - 1);
    while (true)
    {
        Console.Clear();
        Console.WriteLine("===============================");
        Console.WriteLine("          MENU JOUEUR");
        Console.WriteLine("===============================");
        Console.WriteLine(title);
        Console.WriteLine();
        for (int i = 0; i < options.Count; i++)
        {
            var o = options[i];
            var prefix = i == idx ? "> " : "  ";
            var label = o.Label + (o.Enabled ? "" : " (indisponible)");
            Console.WriteLine(prefix + label);
        }
        var k = input.ReadKey();
        if (k == InputKey.Up) idx = (idx - 1 + options.Count) % options.Count;
        else if (k == InputKey.Down) idx = (idx + 1) % options.Count;
        else if (k == InputKey.Enter && options[idx].Enabled) return options[idx];
        else if (k == InputKey.Escape) return null;
    }
}

void ShowPlayerMenu()
{
    while (true)
    {
        var options = new List<MenuOption>
        {
            new("Sauvegarder…", true, "save_menu"),
            new("Inventaire", true, "inv"),
            new("Reprendre", true, "resume"),
            new("Quitter le jeu", true, "quit")
        };
        var sel = ChooseSimple("↑/↓ puis Entrée", options);
        if (sel is null || Equals(sel.Tag, "resume"))
        {
            // resume game
            Render();
            return;
        }
        if (Equals(sel.Tag, "save_menu"))
        {
            while (true)
            {
                var saveOptions = new List<MenuOption>
                {
                    new(FormatSlotPreview("slot1"), true, "slot1"),
                    new(FormatSlotPreview("slot2"), true, "slot2"),
                    new(FormatSlotPreview("slot3"), true, "slot3"),
                    new("Retour", true, "back")
                };
                var pick = ChooseSimple("Choisissez un slot de sauvegarde", saveOptions);
                if (pick is null || Equals(pick.Tag, "back")) break;
                var slotId = (string)pick.Tag!;
                var save = BuildSave() with { SlotId = slotId };
                saveService.Save(slotId, save);
                Console.Clear();
                Console.WriteLine($"Sauvegarde effectuée sur {slotId}.");
                Console.WriteLine($"Carte: {save.MapId}  Pos: ({save.PlayerX},{save.PlayerY})  Heure: {save.SavedAtUtc:HH:mm:ss}");
                Console.WriteLine("Appuyez sur une touche pour revenir au menu.");
                input.ReadKey();
            }
        }
        else if (Equals(sel.Tag, "inv"))
        {
            while (true)
            {
                Console.Clear();
                var invOptions = new List<MenuOption>();
                foreach (var it in inventory.Items)
                {
                    var usable = it.CanUseOn(player);
                    invOptions.Add(new MenuOption(it.Name, usable, it));
                }
                invOptions.Add(new MenuOption("Retour", true, "back"));
                Console.WriteLine("INVENTAIRE — sélectionnez un objet à utiliser (si possible):");
                var pick = ChooseSimple("↑/↓ puis Entrée", invOptions);
                if (pick is null || Equals(pick.Tag, "back")) break;
                var chosen = pick.Tag as Item;
                if (chosen is not null)
                {
                    if (chosen.CanUseOn(player))
                    {
                        chosen.UseOn(player);
                        // Consommer seulement les consommables (ici: potions)
                        if (chosen is PotionHP || chosen is PotionMP || chosen is StatBoostItem)
                            inventory.Remove(chosen);
                        Console.WriteLine($"Vous utilisez {chosen.Name}.");
                        Console.WriteLine("Appuyez sur une touche pour continuer.");
                        input.ReadKey();
                    }
                    else
                    {
                        Console.WriteLine("Objet non utilisable maintenant.");
                        input.ReadKey();
                    }
                }
            }
            Render();
        }
        else if (Equals(sel.Tag, "quit"))
        {
            Environment.Exit(0);
        }
    }
}

// Main loop
while (true)
{
    var key = input.ReadKey();
    if (key == InputKey.Escape)
        break;

    bool moved = false;
    switch (key)
    {
        case InputKey.Up: moved = exploration.TryMove(state, 0, -1); break;
        case InputKey.Down: moved = exploration.TryMove(state, 0, 1); break;
        case InputKey.Left: moved = exploration.TryMove(state, -1, 0); break;
        case InputKey.Right: moved = exploration.TryMove(state, 1, 0); break;
        case InputKey.Menu:
        {
            ShowPlayerMenu();
            continue;
        }
        case InputKey.Enter:
        {
            // Try interactions in priority order: chest -> final door -> door (parc key) -> npc, else feedback
            var lines = new List<string>();
            bool handled = false;

            // Chest reward depends on current map
            Item chestReward = currentMapId switch
            {
                "parc" => new KeyItem(Progression.KeyParc, "Clé du Parc"),
                "labo" => new KeyItem(Progression.KeyLaboratoire, "Clé du Laboratoire"),
                "noyau" => new KeyItem(Progression.KeyNoyau, "Clé du Noyau"),
                _ => new PotionHP(20)
            };
            if (interaction.TryOpenChestAtPlayer(state, inventory, chestReward))
            {
                lines.Add(chestReward is KeyItem
                    ? $"Vous trouvez : {chestReward.Name}. Elle ouvrira une Porte '+'."
                    : $"Vous trouvez : {chestReward.Name}. Ajoutée à l’inventaire.");
                handled = true;
            }
            else if (currentMapId == "noyau" && interaction.TryOpenFinalDoorAtPlayer(state, inventory))
            {
                lines.Add("Vous ouvrez la PORTE FINALE avec vos trois clés !");
                handled = true;
                // Trigger boss battle
                var bossId = enemyCatalog.ContainsKey("final_boss") ? "final_boss" : "bugling";
                var survivedBoss = RunBattleWith(bossId, 7, enemyCatalog, moveCatalog);
                if (survivedBoss)
                {
                    Console.Clear();
                    Console.WriteLine("Le Noyau est purgé. VICTOIRE ! Merci d'avoir joué.");
                    renderer.Present();
                    input.ReadKey();
                    Environment.Exit(0);
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("Vous tombez face au boss du Noyau… Réessayez après vous être renforcé.");
                    renderer.Present();
                    input.ReadKey();
                    Environment.Exit(0);
                }
            }
            else if (currentMapId == "parc" && interaction.TryOpenDoorAtPlayer(state, inventory, Progression.KeyParc))
            {
                lines.Add("Porte: la clé du Parc ouvre la serrure.");
                LoadMapAndRespawn("labo");
                handled = true;
            }
            else if (currentMapId == "labo" && interaction.TryOpenDoorAtPlayer(state, inventory, Progression.KeyLaboratoire))
            {
                lines.Add("Porte: la clé du Laboratoire ouvre la serrure.");
                LoadMapAndRespawn("noyau");
                handled = true;
            }
            else if (interaction.TryTalkToNpcAtPlayer(state, out _, ""))
            {
                // PNJ: Atlas — dialogue variant selon progression (voir lore.md)
                int keyCount = inventory.Items.OfType<KeyItem>().Count();
                var npcLines = new List<string>();
                npcLines.Add("Archiviste Atlas : Ah, un voyageur vivant… Bienvenue dans les Ruines du Terminal.");
                if (keyCount == 0)
                {
                    npcLines.Add("Pour franchir la GRANDE PORTE, il te faudra trois clés : Parc, Laboratoire et Noyau.");
                    npcLines.Add("Les symboles : # mur, . sol, ~ herbe (rencontres), § coffre, @ PNJ, + porte.");
                    npcLines.Add("Commence par fouiller les coffres (§) du Parc. L’herbe ~ attire les Glitches.");
                }
                else if (keyCount == 1)
                {
                    npcLines.Add("Bien vu pour ta première clé. La suivante est dans le Laboratoire : cherche la porte + marquée près d’ici.");
                    npcLines.Add("Reste prudent : les couloirs étroits et les Glitches y sont nombreux.");
                }
                else if (keyCount == 2)
                {
                    npcLines.Add("Il ne te manque plus que la clé du Noyau. Ses pulsations corrompent jusqu’aux herbes.");
                    npcLines.Add("Garde des potions à portée. La Porte finale ne cède qu’aux trois signatures.");
                }
                else
                {
                    npcLines.Add("Tu portes les trois clés. La GRANDE PORTE est prête à s’ouvrir — au-delà, le Noyau.");
                    npcLines.Add("Lorsque tu seras prêt, approche la porte + et engage-toi. Que le Terminal t’accorde la stabilité.");
                }
                lines.AddRange(npcLines);
                handled = true;
            }

            if (!handled)
            {
                // Si une porte est adjacente mais que la clé manque, afficher un message explicite
                var (px, py) = state.PlayerPos;
                bool nearDoor = false;
                var dirs = new (int dx, int dy)[] { (0,0), (1,0), (-1,0), (0,1), (0,-1) };
                foreach (var (dx, dy) in dirs)
                {
                    int nx = px + dx, ny = py + dy;
                    if (ny < 0 || ny >= map.Height || nx < 0 || nx >= map.Width) continue;
                    if (map[ny, nx].Type == TileType.Door) { nearDoor = true; break; }
                }
                if (nearDoor)
                    lines.Add("Porte verrouillée — il vous faut une clé.");
                else
                    lines.Add("Rien à faire ici.");
            }

            // Position the cursor below the HUD, then print the dialogue and wait for a keypress
            SafeSetCursorPosition(0, map.Height + 2);
            renderer.DrawDialogue(lines.ToArray());
            renderer.DrawDialogue("(Appuyez sur une touche pour continuer)");
            renderer.Present();
            input.ReadKey();
            Render();
            continue;
        }
        default:
            break;
    }

    if (moved)
    {
        // After moving onto a tile, check for random encounter trigger (placeholder feedback)
        var t = map[state.PlayerY, state.PlayerX].Type;
        if (exploration.ShouldTriggerEncounter(t))
        {
            Console.Beep(800, 120);
            // Determine encounter (data-driven if available)
            var pick = exploration.PickRandomEncounter(map);
            var enemyId = pick?.EnemyId ?? "bugling";
            var level = pick?.Level ?? 2;

            var survived = RunBattleWith(enemyId, level, enemyCatalog, moveCatalog);
            if (!survived)
            {
                Console.Clear();
                Console.WriteLine("Vous reprenez vos esprits à l'entrée du Parc… (prototype)\nJeu terminé pour cette session.");
                renderer.Present();
                break;
            }
            else
            {
                // Simple reward prototype
                player.GainXp(20);
                inventory.Add(new PotionHP(10));
            }
            Render();
            continue;
        }
        Render();
    }
}

Console.CursorVisible = true;
Console.WriteLine("Merci d'avoir joué !");
