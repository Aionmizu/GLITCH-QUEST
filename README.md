# GLITCH QUEST: Les Ruines du Terminal

Un RPG console ASCII (top‑down) avec combats au tour par tour, rencontres aléatoires, inventaire, et sauvegarde/chargement — conçu proprement (Domain / Application / Infrastructure) et couvert par des tests unitaires.

## Sommaire
- Concept et boucle de jeu
- Architecture et projets
- Prérequis
- Compiler, tester, exécuter
- Données
- Sauvegarde / Chargement
- Qualité & Tests
- Contribution
- Initialisation Git (local) et remote

## Concept (TL;DR)
Dans un ancien système, des « Glitches » corrompent le monde ASCII. Explore des zones, déclenche des rencontres sur l’herbe/corruption, combat en 1v1 au tour par tour, récupère des clés, et restaure le Terminal.

Types: Normal, Feu, Eau, Plante, Électrik (triangle lisible, Normal neutre).

## Architecture
Découpage léger type Clean Architecture:
- Game.Core (Domain): Entités pures et logique (Character, PlayerCharacter, Move, Stats, Items, Map/Tile, EncounterTable, TypeChart…), interfaces (IRandom, ITypeChart, ISaveRepository, IAiStrategy).
- Game.Application: Services orchestrateurs (BattleService, ExplorationService, SaveService), état de jeu (GameStateMachine), IA simple.
- Game.Infrastructure: Implémentations I/O (JsonSaveRepository, FileDataLoader). Pas de Console UI câblée pour l’instant.
- Game (Console): Point d’entrée (bandeau, pas encore d’UI interactive).
- Game.Tests: xUnit + FluentAssertions; tests sur formules de combat, rencontres, sérialisation, IA, leveling.

Arborescence (extrait):
- /src/Game.Core
- /src/Game.Application
- /src/Game.Infrastructure
- /tests/Game.Tests
- /data (moves.json, enemies.json, typechart.json, maps/*.txt)

## Prérequis
- .NET SDK 9.0

## Compiler, tester, exécuter
Depuis la racine du dépôt:
- Build: `dotnet build`
- Tests: `dotnet test`
- Exécution (console app): `dotnet run --project Game.csproj`

Résultat actuel: bandeau d’accueil; les services (combat, exploration, save) sont testés mais l’UI n’est pas connectée.

## Données
- moves.json, enemies.json, typechart.json, et cartes ASCII sous `data/`.

## Sauvegarde / Chargement
- `ISaveRepository` et `JsonSaveRepository` (slot JSON) + `SaveService`.
- Modèle `SaveGame`/`SavePlayer` sérialisable (tests de round‑trip).

## Qualité & Tests
- xUnit + FluentAssertions, tests au vert.
- Viser ≥80% lignes sur BattleService et TypeChart.

## Contribution
- Principe: Domain et Application restent pures (pas d’I/O). L’infrastructure porte le fichier/console.
- Tests unitaires pour toute logique métier ajoutée.
- Style: C# 12, nullable enable.

## Initialisation Git (local) et remote
Ce dépôt doit ignorer les artéfacts de build et la TODO locale. Étapes standard (déjà automatisées dans ce projet):

1) Initialiser (si nécessaire)
   - `git init`
2) S’assurer que la branche par défaut est `main`
   - `git branch -M main`
3) Configurer le remote sur GitHub
   - `git remote add origin https://github.com/Aionmizu/GLITCH-QUEST.git`
4) Premier commit (effectué localement par le script d’initialisation)
5) Ne pas pousser pour l’instant (push à faire plus tard):
   - `git push -u origin main` (à exécuter seulement quand prêt)

Note: Le fichier TODO.md est volontairement exclu du contrôle de version.
