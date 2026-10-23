namespace Voxel.Ui;

public partial class Menu : Node
{
    [Export]
    private Control buttonsControl;

    private List<Node> buttons;

    public override void _EnterTree()
    {
        ChunkManager.IsMenu = true;
    }

    public static void CloseGame()
    {
        ChunkManager.Current._Notification((int)NotificationWMCloseRequest);
        ChunkManager.Current.GetTree().Quit();
    }

    public override void _Ready()
    {
        base._Ready();
        buttons = [.. buttonsControl.GetChildren()];
        ((Button)buttons[0]).ButtonDown += SingleplayerButton;
        ((Button)buttons[1]).ButtonDown += () => { };
        ((Button)buttons[2]).ButtonDown += () => { };
        ((Button)buttons[3]).ButtonDown += CloseGame;

        Dictionary<int, List<ChunkVec3>> list = new() { { 0, [] } };
        for (int x = -3; x < 3; x++) for (int y = -3; y < 3; y++) for (int z = -3; z < 3; z++)
        {
            list[0].Add(new ChunkVec3(x, y, z));
        }

        ChunkManager.SpawnChunksOverride(list);
    }

    private void SingleplayerButton()
    {
        ChunkManager.IsMenu = false;
        ChunkManager.DestroyAllChunksImmediate();
        GetTree().ChangeSceneToFile("res://Scenes/world.tscn");
    }


}
