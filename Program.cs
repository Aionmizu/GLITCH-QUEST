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

// Load default map ("parc") and build exploration state
string currentMapId = "parc";
Map map;
try
{
    var lines = data.ReadMapLines(currentMapId);
    map = exploration.LoadMapFromAscii(lines);
}
catch (Exception ex)
{
    Console.WriteLine($"Erreur de chargement de la carte: {ex.Message}");
    Console.WriteLine("Assurez-vous que data/maps/parc.txt existe.");
    return;
}
var state = new ExplorationState(map);

// Player and inventory (simple defaults)
var player = new PlayerCharacter(
    name: "Hero",
    level: 1,
    type: Element.Fire,
    baseStats: new Stats(30, 10, 6, 5, 5, 1.0, 1.0),
    archetype: Archetype.Balanced
);
// Basic moves — used later when battle UI is wired
player.Moves.Add(new Move { Id = "tackle", Name = "Tacle", Type = Element.Normal, Power = 35, Kind = DamageKind.Physical, Accuracy = 0.98, MpCost = 0, CritChance = 0.05 });
player.Moves.Add(new Move { Id = "ember", Name = "Flammèche", Type = Element.Fire, Power = 40, Kind = DamageKind.Magic, Accuracy = 0.95, MpCost = 3, CritChance = 0.10 });
var inventory = new Inventory();

void DrawHud()
{
    Console.SetCursorPosition(0, map.Height + 1);
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
    state = new ExplorationState(map);
    currentMapId = mapId;
    Render();
}

EnemyCharacter? _currentEnemy = null; // used only for status rendering in Choose()

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


bool RunBattle()
{
    // Create a simple enemy for now (Electric Bugling)
    _currentEnemy = new EnemyCharacter("Bugling", 2, Element.Electric, new Stats(24, 8, 6, 4, 5, 1.0, 1.0), new SimpleAiStrategy());
    _currentEnemy.Moves.Add(new Move { Id = "spark", Name = "Étincelle", Type = Element.Electric, Power = 45, Kind = DamageKind.Physical, Accuracy = 0.90, MpCost = 4, CritChance = 0.10 });

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
            // consume item if used (we remove immediately; effect still applies via BattleService)
            inventory.Remove(item);
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

// Simple Title Menu: New / Load (slot1) / Quit
MenuOption? TitleMenu()
{
    var opts = new List<MenuOption>
    {
        new("Nouvelle Partie", true, "new"),
        new("Charger (slot1)", saveService.Load("slot1") != null, "load1"),
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
        baseStats: new Stats(30, 10, 6, 5, 5, 1.0, 1.0),
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
            new("Sauvegarder (slot1)", true, "save"),
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
        if (Equals(sel.Tag, "save"))
        {
            var save = BuildSave();
            saveService.Save("slot1", save);
            Console.Clear();
            Console.WriteLine("Sauvegarde effectuée sur slot1.");
            Console.WriteLine($"Carte: {save.MapId}  Pos: ({save.PlayerX},{save.PlayerY})  Heure: {save.SavedAtUtc:HH:mm:ss}");
            Console.WriteLine("Appuyez sur une touche pour revenir au jeu.");
            input.ReadKey();
        }
        else if (Equals(sel.Tag, "inv"))
        {
            Console.Clear();
            Console.WriteLine("Inventaire:");
            foreach (var i in inventory.Items)
            {
                Console.WriteLine("- " + i.Name);
            }
            var keyList = inventory.Items.OfType<KeyItem>().Select(k => k.KeyId).ToList();
            Console.WriteLine();
            Console.WriteLine("Clés: " + (keyList.Count == 0 ? "(aucune)" : string.Join(", ", keyList)));
            Console.WriteLine();
            Console.WriteLine("Appuyez sur une touche pour revenir.");
            input.ReadKey();
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

            if (interaction.TryOpenChestAtPlayer(state, inventory, new KeyItem(Progression.KeyParc, "Clé du Parc")))
            {
                lines.Add("Vous trouvez : Clé du Parc. Elle ouvrira une Porte '+'.");
                handled = true;
            }
            else if (interaction.TryOpenFinalDoorAtPlayer(state, inventory))
            {
                lines.Add("Vous ouvrez la porte finale avec vos trois clés !");
                handled = true;
            }
            else if (interaction.TryOpenDoorAtPlayer(state, inventory, Progression.KeyParc))
            {
                lines.Add("Porte: la clé du Parc ouvre la serrure.");
                LoadMapAndRespawn("labo");
                handled = true;
            }
            else if (interaction.TryTalkToNpcAtPlayer(state, out _, ""))
            {
                // PNJ: vrai dialogue avec lore et conseils. Voir lore.md pour les détails.
                var npcLines = new[]
                {
                    "Archiviste Atlas : Ah, un voyageur vivant… Bienvenue dans les Ruines du Terminal.",
                    "Pour franchir la GRANDE PORTE, il te faudra trois clés : Parc, Laboratoire et Noyau.",
                    "Les symboles du monde : # mur, . sol, ~ herbe (rencontres), § coffre, @ PNJ, + porte.",
                    "Cherche les coffres et écoute le bourdonnement des Glitches dans l’herbe.",
                    "Reviens me voir quand tu auras une clé — je te dirai où aller ensuite."
                };
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
            Console.SetCursorPosition(0, map.Height + 2);
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
            // Launch the actual battle loop
            var survived = RunBattle();
            if (!survived)
            {
                Console.Clear();
                Console.WriteLine("Vous reprenez vos esprits à l'entrée du Parc… (prototype)\nJeu terminé pour cette session.");
                renderer.Present();
                break;
            }
            Render();
            continue;
        }
        Render();
    }
}

Console.CursorVisible = true;
Console.WriteLine("Merci d'avoir joué !");
