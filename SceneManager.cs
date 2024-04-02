using Godot;
using System;

public partial class SceneManager : Node3D
{
	private PackedScene playerPrefab;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		playerPrefab = GD.Load<PackedScene>("res://assets3D/prefabs3D/player_3d.tscn");
		if (this.HasNode("/root/"+this.Name+"/player3D")){
			//GD.Print("Player already loaded");
			
		}
		else{
			//GD.Print("Player not found");
			SpawnAtEntrance();
		}
	}

	private void SpawnAtEntrance(){
		//var lvlmngr = (LevelManager) GetTree().Root.GetChild(0);
		//GD.Print(lvlmngr.PreviousScene.Name);
		var entrances = this.GetNode<Node3D>("Entrances");

		foreach (Node3D child in entrances.GetChildren()){
			if (child.Name == GetNode<LevelManager>("/root/LevelManager").PreviousScene){
				//GD.Print("Entrance found");
				var playerInstance = playerPrefab.Instantiate<player3D>();
				this.AddChild(playerInstance);
				playerInstance.Set(Node3D.PropertyName.Position, child.Position);
			}
			else{
				//GD.Print("No previous level detected spawning in first entrance");
				var playerInstance = playerPrefab.Instantiate<player3D>();
				this.AddChild(playerInstance);
				playerInstance.Set(Node3D.PropertyName.Position, ((Node3D)entrances.GetChild(0)).Position);
			}
		}
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
