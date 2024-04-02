using Godot;
using System;

public partial class player3D : CharacterBody3D
{

	//Date: 3/24/2024 planned finishing more of the shared kit for all characters.
	public enum playerabilities{
		dash,
		doublejump,
		walljump,
		rollout,
		roar
	}

	[Export]
	public float dashspeed = 15f;
	[Export]
	public float maxDashTimer = 0.2f;
	public float dashTimer;
	private int dashdirection;
	[Export]
	public float maxDashCooldown = .8f;
	private float dashCooldown = 0f;
	private bool canDash = true;

	[Export]
	public float overspeed;
	private bool slidejump;

	//end of 3/26/2024 updates


	[Export]
	private float wallSlideSpeed = -7f;
	public bool onWallLeft = false;
	public bool onWallRight = false;

	[Export]
	public float wallJumpXVelocity = 10f;
	[Export]
	private float wallJumpYVelocity = 13f;
	[Export]
	private float maxWallJumpFuel = 0.3f;
	private float wallJumpFuel = 0f;
	public bool wallJumping = false;
	
	[Export]
	private float gravMod = 1.2f;

	[Export]
	public float speed = 11.0f;
	private float fuel = 0f;
	private float pogoFuel = 0.2f;
	[Export]
	private float maxFuel = 0.3f;
	bool fly = false;
	public bool jumpReleased = true;
	[Export]
	public float jumpVelocity = 11.0f;

	private bool toggleSurf = false;
	//private float pseudoInertia = 0f;
	//private float inertia = 0f;
	//private float velocityThreshold = 10000f;
	//private bool hasInertia = false;
	//private Vector2 inertiaVector = Vector2.Zero;
	private float slopeSpeedMod = 0f;

	[Export]
	private int maxFloorAngle = 10;
	private float decelMod = 20f;

	private bool sliding = false; //used for communicating with incline detector
	private InclineDetector3D inclineDetector;
	//private Sprite3D SlideIndicator;
	private WallDetector3D LeftWallDetector;
	private WallDetector3D RightWallDetector;

	[Export]
	public float knockbackAmount = 1.2f;
	private float knockbackTime;
	[Export]
	private float maxKnockbackTime = 0.2f;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	private string location;
	public override void _Ready()
	{
		LeftWallDetector = GetNode<WallDetector3D>("LeftWallDetector");
		RightWallDetector = GetNode<WallDetector3D>("RightWallDetector");
		gravity = gravity*gravMod;
		inclineDetector = GetNode<InclineDetector3D>("InclineDetector");
		//SlideIndicator = GetNode<Sprite3D>("SlideIndicator");
		//boxCollider = GetNode<CollisionShape2D>("BoxCollider");
		//boxColliderTemp = GetNode<CollisionShape2D>("BoxCollider");
	}

	public float GetFuel(){
		return fuel;
	}
	public bool GetSlide(){
		return sliding;
	}	

	public void Pogo(){
		fly = true;
		fuel = pogoFuel;
		canDash = true;
		//this function is necessary because the attack script is separate from the movement handling system
	}

	public void OnHitPlayerKnockback(int knockbackdirection){
		knockbackTime = maxKnockbackTime;
		knockbackAmount = Mathf.Abs(knockbackAmount) * knockbackdirection;
		GD.Print("player3D.cs: Onhitplayerknockback signal received");
		
		//This is a super not good way of doing this
		//this candash bit is essential to the dash mechanic working with the attack system.
		canDash = true;
	}

	// For later uses...
	public Godot.Collections.Dictionary<string, Variant> Save()
	{
		return new Godot.Collections.Dictionary<string, Variant>()
		{
			{ "Filename", SceneFilePath },
			{ "Parent", GetParent().GetPath() },
			{ "Location", location}
		};
	}
	//

	public override void _PhysicsProcess(double delta)
	{

		Vector3 velocity = Velocity;

		//basic jump mechanics
		if (fly && fuel > 0){
			fuel -= (float) delta;
			velocity.Y = jumpVelocity;
		}
		if (fuel <= 0){
			fly = false;
		}
		if (IsOnFloor()){
			fuel = maxFuel;
			toggleSurf = false;
			canDash = true;
		}

		//solves uncapped falling jump
		if (!IsOnFloor() && Input.IsActionJustPressed("jump")){
			//air jump -1
			//fuel = maxSecondaryfuel
			fuel = 0;
		}

		//skipped double jump and wall jump functionality 9/25/2023
		//skipped double jump functionality 10/29/2023, prepping for wall jump
		if (Input.IsActionPressed("jump") && jumpReleased){
			if (fuel == maxFuel){
				fly = true;
				//again double jump functionality is skipped
			}
			if (jumpReleased){
				jumpReleased = false;
			}
		}
		if (Input.IsActionJustReleased("jump")){
			fly = false;
			jumpReleased = true;
		}
		
		// Add the gravity.
		if (!IsOnFloor()){
			velocity.Y -= gravity* (float)delta;
		}


		//Wall sliding logic, see wall detector 3D.
		onWallLeft = LeftWallDetector.OnWallLeft;
		onWallRight = RightWallDetector.OnWallRight;

		if ((onWallLeft || onWallRight) && !wallJumping){
			velocity = new Vector3(velocity.X, wallSlideSpeed, 0);
			fuel = 0;
			canDash = true;
		}

		if (onWallLeft){
			if (Input.IsActionJustPressed("jump")){
				wallJumping = true;
				wallJumpFuel = maxWallJumpFuel;
				wallJumpXVelocity = Mathf.Abs(wallJumpXVelocity);
			}
		}
		if (onWallRight){
			if (Input.IsActionJustPressed("jump")){
				wallJumping = true;
				wallJumpFuel = maxWallJumpFuel;
				wallJumpXVelocity = -Mathf.Abs(wallJumpXVelocity);
			}
		}

		if (wallJumpFuel > 0){
			velocity = new Vector3(wallJumpXVelocity, wallJumpYVelocity, 0);
			wallJumpFuel -= (float) delta;
		}
		else{
			wallJumping = false;//prevents momentum carrying over out of wall jump
		}
		if (wallJumping && Input.IsActionJustReleased("jump") && jumpReleased){//allows for elbow climbing
			wallJumpFuel = 0;
			wallJumping = false;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "ui_up", "ui_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (wallJumping){
			direction[0] = 0;//wall jumping contradiction
		}

		//SLIDE HANDLING----------------------------------------------------

		if (inclineDetector.OnSlide)
		{
			var v = inclineDetector.inclineVector;
			bool positiveSlantAngle = false;
			if (v.X < 0)
			{
				slopeSpeedMod = Mathf.Abs(v.Y);//The steeper the hill the faster you walk down
				positiveSlantAngle = false;
			}
			if (v.X > 0)
			{
				slopeSpeedMod = Mathf.Abs(v.Y);
				positiveSlantAngle = true;
			}
			if (v.X == 0)
			{
				slopeSpeedMod = 0f;
			}
			
			/*
			Seamless slope handling
			Jump velocity is negative.
			var slideAngle = inclineDetector.inclineVector;//[x,y], sin(theta)=x, cos(theta)=y
			left, right: [-1, 1]
			up, down: [-1, 1]
			if slant angle is positive: moveleft > -x,-y || moveright > x,y
			if slant angle is negative: moveleft > -x,y || moveright > x,-y
			Velocity = new Vector2(inclineVector.Y, inclineVector.X);

			*/
			if (direction != Vector3.Zero)
			{
				velocity.X = direction.X * speed;
				if (positiveSlantAngle){
					if (velocity.X > 0){
						velocity.X = direction.X * Mathf.Abs(v.X) * speed *(slopeSpeedMod-1);
						if (!Input.IsActionPressed("jump") && IsOnFloor()){
							velocity.Y = Mathf.Abs(v.Y) * speed *(slopeSpeedMod-1.2f);
						}
					}
					else{
						//move and slide will suffice as you walk up a hill
						velocity.X = direction.X * speed;
					}
				}
				if (!positiveSlantAngle)
				{
					if (velocity.X > 0){
						//move and slide will suffice as you walk up a hill
						velocity.X = direction.X * speed;
					}
					else{
						velocity.X = direction.X * Mathf.Abs(v.X) * speed *(slopeSpeedMod-1);
						if (!Input.IsActionPressed("jump") && IsOnFloor()){
							velocity.Y = Mathf.Abs(v.Y) * speed *(slopeSpeedMod-1.2f);
						}
						
					}
				}
			}
			else
			{
				if (!wallJumping){
					velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);//wall jumping contradiction
				}
			}
		}
		else//not on slide
		{
			if (direction != Vector3.Zero)
			{//actually straight up broken logic because surface underside
				velocity.X = direction.X * speed;
				//GD.Print("Case1");
			}
			else
			{
				if (!wallJumping){
					velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);//wall jumping contradiction
				}
				//GD.Print("Case2");
			}
		}
		//**********************
		sliding = toggleSurf;

		if (Input.IsActionPressed("look_down") && !IsOnFloor()){
			toggleSurf = true;//really ghetto, not using events such as falling off a ledge
			//tutorial "HOLD SURF WHEN LANDING TO INITIATE A SURF"
			//SlideIndicator.Visible = true;
		}

		if (!inclineDetector.OnSlide && toggleSurf){
			toggleSurf = false;
			//SlideIndicator.Visible = false;
		}

		if (inclineDetector.OnSlide && toggleSurf && Input.IsActionJustPressed("look_down"))
		{
			toggleSurf = false;
		}

		//Yeah this is separate here i think
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * speed;
			//velocity.Z = direction.Z * Speed;
		}
		else
		{
			if (!wallJumping){
				velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);//wall jumping contradiction
			}
			
			//velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		//dash changes 3/26/2024
		//TODO implement 1 dash and dash reset logic
		if (inclineDetector.OnSlide){
			canDash = true;
		}

		if (onWallLeft && dashCooldown <= 0){
			dashdirection = 1;
		}
		else if (onWallRight && dashCooldown <= 0){
			dashdirection = -1;
		}
		else if (direction.X > 0 && dashTimer <= 0){
			dashdirection = 1;
		}
		else if (direction.X < 0 && dashTimer <= 0){
			dashdirection = -1;
		}
		else if (Input.IsActionJustPressed("move_left") && dashTimer <= 0){
			dashdirection = -1;	
		}
		else if (Input.IsActionJustPressed("move_right") && dashTimer <= 0){
			dashdirection = 1;
		}
		if (dashCooldown > 0){
			dashCooldown -= (float) delta;
		}
		if (Input.IsActionJustPressed("dash") && dashCooldown <= 0 && canDash){
			
			dashCooldown = maxDashCooldown;
			dashTimer = maxDashTimer;
			canDash = false;

			//Again bad practice of interweving this into the dash system
			knockbackTime = 0;
		}
		if (dashTimer > 0){
			fuel = 0;
			dashTimer -= (float) delta;
			velocity.X = dashdirection * dashspeed;
			velocity.Y = 0;
		}

		//knockback handling 3/17/2024
		if (knockbackTime >= 0){
			knockbackTime -= (float) delta;
			velocity.X = knockbackAmount;
			fuel = 0;
		}

		if (inclineDetector.OnSlide && toggleSurf)
		{
			if (Input.IsActionPressed("jump")){
				//GD.Print("Slide jump");
				toggleSurf = false;
				fuel = maxFuel;
				fly = true;
				
			}
			this.FloorMaxAngle = Mathf.DegToRad(1);
			//need timer;
			//toggle surf?
			//These leaning and breaking function just off of the move and slide and direction
			if (inclineDetector.inclineVector.X > 0){
				if (direction.X > 0){
					//lean
					//GD.Print("Leaning on positive slope");
				}
				else if (direction.X < 0){
					//break
					//GD.Print("Breaking on positive slope");
				}
				
			}
			if (inclineDetector.inclineVector.X < 0){
				if (direction.X < 0){
					//lean
					//GD.Print("Leaning on negative slope");
				}
				else if (direction.X > 0){
					//break
					//GD.Print("Breaking on negative slope");
				}
			}
		}
		else
		{
			this.FloorMaxAngle = Mathf.DegToRad(maxFloorAngle);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
