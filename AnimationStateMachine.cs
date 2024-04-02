using Godot;
using System;

public partial class AnimationStateMachine : Node3D
{

	#region //all state and sub state machine variables
	private AnimationNodeStateMachinePlayback MainStateMachine;
	//Attacking state and sub state machines
	private AnimationNodeStateMachinePlayback AttackStateMachine;
	private AnimationNodeStateMachinePlayback AirborneAttackStateMachine;
	private AnimationNodeStateMachinePlayback GroundedAttackStateMachine;
	private AnimationNodeStateMachinePlayback WallAttackStateMachine;
	//Movement state and sub state machines
	private AnimationNodeStateMachinePlayback MovementStateMachine;
	private AnimationNodeStateMachinePlayback AirborneMovementStateMachine;
	private AnimationNodeStateMachinePlayback GroundedMovementStateMachine;
	private AnimationNodeStateMachinePlayback WallMovementStateMachine;
	//interaction machine
	private AnimationNodeStateMachinePlayback InteractionStateMachine;
	private AnimationTree tree;
	#endregion

	public player3D playerController;
	public Sprite3D AnimatedSprite;

	[Export]
	private float maxAttackCooldown = 0.55f;
	private float attackCooldown;
	private bool basicAttackReady = true;

	[Export]
	private float maxIdlePanTimer = 1f;
	private float idlePanTimer;
	
	[Signal]
	public delegate void PanCameraEventHandler();

	//misc conditions vars
	private GpuParticles3D landingParticles;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CallDeferred(nameof(LoadAnimationStateMachines));

		playerController = (player3D) this.GetParent();
		AnimatedSprite = this.GetNode<Sprite3D>("AnimationSprite");
		
		landingParticles = this.GetNode<GpuParticles3D>("LandingParticles");

		GD.Print("AnimationStateMachine.cs: TODO Connect pan camera event to the physics controller and to the camera manager");
	
	}

	private void LoadAnimationStateMachines(){

		#region //initialize all state and sub state machines
		tree = GetNode<AnimationTree>("TestTree");
		//main
		MainStateMachine = tree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
		//attacking
		AttackStateMachine = tree.Get("parameters/Attack state machine/playback").As<AnimationNodeStateMachinePlayback>();
		//GD.Print("Animation state machine: Attack state machine, " + (AttackStateMachine == null));
		AirborneAttackStateMachine = tree.Get("parameters/Attack state machine/Airborne attack state machine/playback").As<AnimationNodeStateMachinePlayback>();
		//GD.Print("Animation state machine: airborne attack state machine, " + (AirborneAttackStateMachine == null));
		GroundedAttackStateMachine = tree.Get("parameters/Attack state machine/Grounded attack state machine/playback").As<AnimationNodeStateMachinePlayback>();
		//GD.Print("Animation state machine: grounded attack state machine, " + (GroundedAttackStateMachine == null));
		WallAttackStateMachine = tree.Get("parameters/Attack state machine/Wall attack state machine/playback").As<AnimationNodeStateMachinePlayback>();
		//GD.Print("Animation state machine: wall attack state machine, " + (WallAttackStateMachine == null));
		//movement
		MovementStateMachine = tree.Get("parameters/Movement state machine/playback").As<AnimationNodeStateMachinePlayback>();
		//GD.Print("Animation state machine: movement state machine, " + (MovementStateMachine == null));
		AirborneMovementStateMachine = tree.Get("parameters/Movement state machine/Airborne movement state machine/playback").As<AnimationNodeStateMachinePlayback>();
		//GD.Print("Animation state machine: airborne movement state machine, " + (AirborneMovementStateMachine == null));
		GroundedMovementStateMachine = tree.Get("parameters/Movement state machine/Grounded movement state machine/playback").As<AnimationNodeStateMachinePlayback>();
		//GD.Print("Animation state machine: grounded movement state machine, " + (GroundedMovementStateMachine == null));
		WallMovementStateMachine = tree.Get("parameters/Movement state machine/Wall movement state machine/playback").As<AnimationNodeStateMachinePlayback>();
		//GD.Print("Animation state machine: wall movement state machine, " + (WallAttackStateMachine == null));
		//interaction
		InteractionStateMachine = tree.Get("parameters/Interaction state machine/playback").As<AnimationNodeStateMachinePlayback>();
		//GD.Print("Animation state machine: interaction state machine, " + (InteractionStateMachine == null));
		#endregion

	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (attackCooldown <= 0){
			basicAttackReady = true;
		}
		if (attackCooldown > 0){
			attackCooldown -= (float) delta;
		}

		if ((MainStateMachine.GetCurrentNode() != "Attack state machine") && 
		!(playerController.onWallLeft || playerController.onWallRight)) {
			if (Input.IsActionPressed("move_left")){
				AnimatedSprite.FlipH = true;
				//ApplyScale(new Vector3(-1,1,1));
			}
			if (Input.IsActionPressed("move_right")){
				AnimatedSprite.FlipH = false;
				//ApplyScale(new Vector3(-1,1,1));
			}
		}
		if (playerController.onWallLeft){
			AnimatedSprite.FlipH = false;
		}
		if (playerController.onWallRight){
			AnimatedSprite.FlipH = true;
		}

		#region //Airborne movement statemachine
		if (!playerController.IsOnFloor() && !playerController.GetSlide()){

			MovementStateMachine.Set("conditions/airborne", true);
			MovementStateMachine.Travel("Airborne movement state machine");
			GroundedMovementStateMachine.Set("conditions/airborne", true);

			if (Input.IsActionJustPressed("debug")){
				GD.Print("AnimationStateMachine.cs: " + MovementStateMachine.GetCurrentNode());
			}

			if (Input.IsActionPressed("jump") && playerController.GetFuel() > 0){
				AirborneMovementStateMachine.Set("conditions/jumping", true);
				AirborneMovementStateMachine.Travel("jump");
			}
			else{
				AirborneMovementStateMachine.Set("conditions/jumping", false);
			}

			if (playerController.GetFuel() <= 0 || !Input.IsActionPressed("jump")){
				AirborneMovementStateMachine.Set("conditions/falling", true);
				AirborneMovementStateMachine.Travel("fall");
			}
			else{
				AirborneMovementStateMachine.Set("conditions/falling", false);
			}

			if (playerController.onWallLeft || playerController.onWallRight){
				AirborneMovementStateMachine.Set("conditions/onwall", true);
				AirborneMovementStateMachine.Travel("wall (transitional node)");
				MovementStateMachine.Set("conditions/onwall", true);
				MovementStateMachine.Travel("Wall movement state machine");
			}

			#region //Airborne attack state machine
			if (Input.IsActionPressed("attack") && basicAttackReady){
				AirborneMovementStateMachine.Set("conditions/attacking", true);
				MovementStateMachine.Set("conditions/attacking", true);
				MainStateMachine.Set("conditions/attacking", true);
				MainStateMachine.Travel("Attack state machine");
				AttackStateMachine.Set("conditions/airborne", true);
				AttackStateMachine.Travel("Airborne attack state machine");

				if (Input.IsActionPressed("look_down")){
					AirborneAttackStateMachine.Set("conditions/down", true);
					AirborneAttackStateMachine.Travel("down air");
					basicAttackReady = false;
					attackCooldown = maxAttackCooldown;
					
				}
				else if (Input.IsActionPressed("look_up")){
					AirborneAttackStateMachine.Set("conditions/up", true);
					AirborneAttackStateMachine.Travel("up air");
					basicAttackReady = false;
					attackCooldown = maxAttackCooldown;
				}
				else{
					AirborneAttackStateMachine.Set("conditions/forward", true);
					AirborneAttackStateMachine.Travel("forward air");
					basicAttackReady = false;
					attackCooldown = maxAttackCooldown;
				}
				AirborneAttackStateMachine.Set("conditions/forward", false);
				AirborneAttackStateMachine.Set("conditions/down", false);
				AirborneAttackStateMachine.Set("conditions/up", false);

			}
			else{
				AirborneMovementStateMachine.Set("conditions/attacking", false);
				MovementStateMachine.Set("conditions/attacking", false);
				MainStateMachine.Set("conditions/attacking", false);
				AttackStateMachine.Set("conditions/airborne", false);
			}
			#endregion//ending airborne attack state machine
		}
		else{
			MovementStateMachine.Set("conditions/airborne", false);
			GroundedMovementStateMachine.Set("conditions/airborne", false);
		}
		#endregion//ending airborne movement state machine


		#region //grounded movement statemachine.
		if (playerController.IsOnFloor() || playerController.GetSlide()){
			
			AirborneMovementStateMachine.Set("conditions/grounded", true);
			MovementStateMachine.Set("conditions/grounded", true);
			MovementStateMachine.Travel("Grounded movement state machine");
			

			if (!(Input.IsActionPressed("move_left") ^ Input.IsActionPressed("move_right")) ||
			(Input.IsActionPressed("move_left") && Input.IsActionPressed("move_right"))){
				GroundedMovementStateMachine.Set("conditions/idle", true);
				GroundedMovementStateMachine.Travel("idle");
			}
			else{
				GroundedMovementStateMachine.Set("conditions/idle", false);
			}

			if (MainStateMachine.GetCurrentNode() == "idle"){
				idlePanTimer -= (float) delta;
			}
			else{
				idlePanTimer = maxIdlePanTimer;
			}
			if (idlePanTimer <= 0){
				if (Input.IsActionPressed("look_up") && !Input.IsActionPressed("look_down")){
					GroundedMovementStateMachine.Set("conditions/lookup", true);
					GD.Print("AnimationStateMachine.cs: Missing animation for idle pan up");
				}
				else{
					GroundedMovementStateMachine.Set("conditions/lookup", false);
				}
				if (Input.IsActionPressed("look_down") && !Input.IsActionPressed("look_up")){
					GroundedMovementStateMachine.Set("conditions/lookdown", true);
					GD.Print("AnimationStateMachine.cs: Missing animation for idle pan down");
				}
				else{
					GroundedMovementStateMachine.Set("conditions/lookdown", false);
				}
			}

			if (Input.IsActionPressed("move_left") ^ Input.IsActionPressed("move_right")){
				GroundedMovementStateMachine.Set("conditions/running", true);
				GroundedMovementStateMachine.Travel("run");
			}
			else{
				GroundedMovementStateMachine.Set("conditions/running", false);
			}

			if (Input.IsActionJustPressed("debug")){
				GD.Print("AnimationStateMachine.cs: " + GroundedMovementStateMachine.GetCurrentNode());
			}

			#region //Grounded attack state machine
			if (Input.IsActionPressed("attack") && basicAttackReady){
				GroundedMovementStateMachine.Set("conditions/attacking", true);
				MovementStateMachine.Set("conditions/attacking", true);
				MainStateMachine.Set("conditions/attacking", true);
				MainStateMachine.Travel("Attack state machine");
				AttackStateMachine.Set("conditions/grounded", true);
				AttackStateMachine.Travel("Grounded attack state machine");

				if (Input.IsActionPressed("look_up")){
					GroundedAttackStateMachine.Set("conditions/up", true);
					GroundedAttackStateMachine.Travel("up grounded");
					basicAttackReady = false;
					attackCooldown = maxAttackCooldown;
				}
				else if (!Input.IsActionPressed("look_up")){
					GroundedAttackStateMachine.Set("conditions/forward1", true);
					GroundedAttackStateMachine.Travel("forward string 1");
					basicAttackReady = false;
					attackCooldown = maxAttackCooldown;
				}
				GroundedAttackStateMachine.Set("conditions/up", false);
				GroundedAttackStateMachine.Set("conditions/forward1", false);
				
			}
			else{
				GroundedMovementStateMachine.Set("conditions/attacking", false);
				MovementStateMachine.Set("conditions/attacking", false);
				MainStateMachine.Set("conditions/attacking", false);
				AttackStateMachine.Set("conditions/grounded", false);
			}
			#endregion//grounded attack state machine
		}
		else{
			MovementStateMachine.Set("conditions/grounded", false);
		}
		#endregion//grounded movement state machine

		#region //Wall movement state machine
		if (WallMovementStateMachine.GetCurrentNode() == "wall slide"){
			if (Input.IsActionJustPressed("jump")){
				WallMovementStateMachine.Set("conditions/wallJump", true);
				WallMovementStateMachine.Travel("wall jump");
			}
			else{
				WallMovementStateMachine.Set("conditions/wallJump", true);
			}

			if (!playerController.onWallLeft && !playerController.onWallRight && !playerController.IsOnFloor()){
				WallMovementStateMachine.Set("conditions/airborne", true);
				WallMovementStateMachine.Travel("airborne (transitional node)");
			}
			else{
				WallMovementStateMachine.Set("conditions/airborne", false);
			}

			if (playerController.IsOnFloor()){
				WallMovementStateMachine.Set("conditions/grounded", true);
				WallMovementStateMachine.Travel("grounded (transitional node)");
			}
			else{
				WallMovementStateMachine.Set("conditions/grounded", false);
			}
			
		}
		if (MovementStateMachine.GetCurrentNode() == "Wall movement state machine"){
			if (Input.IsActionJustPressed("attack")){
				WallMovementStateMachine.Set("conditions/attacking", true);
				WallMovementStateMachine.Travel("attacking (transitional node)");
			}
			else{
				WallMovementStateMachine.Set("conditions/attacking", false);
			}
		}
		#endregion//end wall movement state machine
		
		//MISC other code
		if (MovementStateMachine.GetCurrentNode() == "land"){
			landingParticles.Emitting = true;
		}
	}
}
