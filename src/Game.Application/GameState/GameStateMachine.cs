namespace Game.Application.GameState;

public enum GameState
{
    MainMenu,
    Exploration,
    Battle,
    Inventory,
    SaveLoad,
    Dialogue
}

/// <summary>
/// Minimal state machine skeleton with a current state and simple transitions.
/// No I/O here; orchestration only. UI/infrastructure will drive it later.
/// </summary>
public sealed class GameStateMachine
{
    public GameState State { get; private set; } = GameState.MainMenu;

    public void StartNewGame()
    {
        State = GameState.Exploration;
    }

    public void EnterBattle() => State = GameState.Battle;
    public void ExitBattleToExploration() => State = GameState.Exploration;

    public void OpenInventory() => State = GameState.Inventory;
    public void CloseInventoryToExploration() => State = GameState.Exploration;

    public void OpenSaveLoad() => State = GameState.SaveLoad;
    public void CloseSaveLoadToExploration() => State = GameState.Exploration;

    public void OpenDialogue() => State = GameState.Dialogue;
    public void CloseDialogueToExploration() => State = GameState.Exploration;
}
