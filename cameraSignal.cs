using Godot;
using System;

public partial class cameraSignal : ShapeCast3D 
{
	public bool activesignal = false;
	
	public enum signaltype{
		fixedxy,
		fixedx,
		fixedy,
		boundx,
		boundy
	}

	[Export]
	public Vector3 focusoffset = Vector3.Zero;
	
	[Export]
	public bool min0_max1;
	//True bounds +x, false bounds -x
	//True bounds +y, false bounds -y
	private CameraManager cameraManager;

	[Signal]
	public delegate void OnEnterFixCameraXYEventHandler(float xhold, float yhold);
	[Signal]
	public delegate void OnEnterFixCameraXEventHandler(float xhold);
	[Signal]
	public delegate void OnEnterFixCameraYEventHandler(float yhold);
	[Signal]
	public delegate void OnEnterAdjustZoomEventHandler(float z);

	//Slightly different from X and Y camera fix.
	//These essentially bound the camera in 1 direction.
	//Fix camera X is equivalent to binding the camera from the left and right.
	//Bound signals trigger a minima comparison between camera position and the focus.
	[Signal]
	public delegate void OnEnterBoundCameraXEventHandler(bool right);
	[Signal]
	public delegate void OnEnterBoundCameraYEventHandler(bool up);

	[Signal]
	public delegate void OnExitReleaseCameraEventHandler();
	

	[Export]
	public signaltype SelectedOption {get; set; } = signaltype.fixedxy;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		cameraManager = this.GetParent().GetParent().GetNode<CameraManager>("CameraManager");

		this.Connect("OnExitReleaseCamera", new Godot.Callable(cameraManager, "DequeueCameraFix"));

		if (this.focusoffset.Z != 0){
			this.Connect("OnEnterAdjustZoom", new Godot.Callable(cameraManager, "AdjustZoom"));
		}
		if (SelectedOption == signaltype.fixedxy){
			this.Connect("OnEnterFixCameraXY", new Godot.Callable(cameraManager, "FixCameraXY"));
			//EmitSignal(SignalName.OnEnterFixCameraXY, this.Position.X, this.Position.Y);
		}
		if (SelectedOption == signaltype.fixedx){
			this.Connect("OnEnterFixCameraX", new Godot.Callable(cameraManager, "FixCameraX"));
			//EmitSignal(SignalName.OnEnterFixCameraX, this.Position.X);
		}
		if (SelectedOption == signaltype.fixedy){
			this.Connect("OnEnterFixCameraY", new Godot.Callable(cameraManager, "FixCameraY"));
			//EmitSignal(SignalName.OnEnterFixCameraY, this.Position.Y);
		}
		
		if (SelectedOption == signaltype.boundx){
			this.Connect("OnEnterBoundCameraX", new Godot.Callable(cameraManager, "BoundCameraX"));
		}
		if (SelectedOption == signaltype.boundy){
			this.Connect("OnEnterBoundCameraY", new Godot.Callable(cameraManager, "BoundCameraY"));
		}
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (this.IsColliding() && !activesignal){
			activesignal = true;

			if (this.focusoffset.Z != 0){
				EmitSignal(SignalName.OnEnterAdjustZoom, focusoffset.Z);
			}

			if (SelectedOption == signaltype.fixedxy){
				EmitSignal(SignalName.OnEnterFixCameraXY, focusoffset.X + this.Position.X,
				 focusoffset.Y + this.Position.Y);
				//GD.Print("Pinning camera");
			}
			if (SelectedOption == signaltype.fixedx){
				EmitSignal(SignalName.OnEnterFixCameraX, focusoffset.X + this.Position.X);
				//GD.Print("Pinning camera");
			}
			if (SelectedOption == signaltype.fixedy){
				EmitSignal(SignalName.OnEnterFixCameraY, focusoffset.Y + this.Position.Y);
				//GD.Print("Pinning camera");
			}

			if (SelectedOption == signaltype.boundx){
				EmitSignal(SignalName.OnEnterBoundCameraX, focusoffset.X + this.Position.X, min0_max1);
			}
			if (SelectedOption == signaltype.boundy){
				EmitSignal(SignalName.OnEnterBoundCameraX, focusoffset.Y + this.Position.Y, min0_max1);
			}
		}
		if (activesignal && !this.IsColliding()){
			EmitSignal(SignalName.OnExitReleaseCamera);
			activesignal = false;
			//GD.Print("Collision ended, removing camera pin");
		}
	}
}
