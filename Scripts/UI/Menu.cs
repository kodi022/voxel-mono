using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Voxel;

public partial class Menu : Node
{
    [Export]
    private Control buttonsControl;

    private List<Node> buttons;

    public override void _Ready()
    {
        base._Ready();
        buttons = [.. buttonsControl.GetChildren()];
        ((Button)buttons[0]).ButtonDown += SingleplayerButton;
        ((Button)buttons[1]).ButtonDown += () => { };
        ((Button)buttons[2]).ButtonDown += () => { };
        ((Button)buttons[3]).ButtonDown += ChunkManager.CloseGame;
    }

    private void SingleplayerButton()
    {
        GetTree().ChangeSceneToFile("res://Scenes/world.tscn");
    }
}
