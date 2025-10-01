using Game.Core.Domain;

namespace Game.Core.Abstractions;

public interface IRenderer
{
    void Clear();
    void DrawMap(Map map, int playerX, int playerY);
    void DrawDialogue(params string[] lines);
    void DrawBattleStatus(Character a, Character b);
    void Present();
}
