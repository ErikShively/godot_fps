using Godot;
using System;
using HandleInput;

public class Player : KinematicBody
{
	[Export]
	public float MaxSpeed = 5;
	[Export]
	public float WalkAccelerationScale = 15;
	[Export]
	public float WalkFrictionScale = 15;
	public float FrictionScale = 1.5f;
	[Export]
	public float MouseSensitivityX = 5;
	[Export]
	public float MouseSensitivityY = 5;
	[Export]
	public float FallAccel = 15.5f;
	[Export]
	public float MaxFallSpeed = 60f;
	[Export]
	public float StopThreshold = 0.05f;
	private KinematicCollision CollisionInfo = new KinematicCollision();

	private Camera ViewCamera = new Camera();

	private Vector2 MouseDelta = new Vector2();

	InputHandler InputHandle = new InputHandler();
	private Vector3 InputVector = Vector3.Zero;
	private Vector3 ViewAngle = new Vector3();

	private Vector3 RelativeVelocity = new Vector3();
	private Vector3 WalkVelocity = new Vector3();
	private Vector3 InheretedVelocity = new Vector3();
	private Vector3 Velocity = new Vector3();

	private Vector3 Impulse = new Vector3();
	private Vector3 NetForce = new Vector3();
	private Vector3 ExternalForce = new Vector3();

	private MovementStates MovementState;
	private MovementStates PreviousState;
	private enum MovementStates{
		GroundedWalk,
		Falling,
	}
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Movement functions

// 	Behaves like a falling state. All impulse should induce a falling state. There should be a persistent impluse property across functions.
// 	Each function should trigger impulse within itself, maybe with a condition of checking impulse on every call.
// 	A static impulse will add a set amount of force to a system
	public void SetImpulse(Vector3 InImpulse, float delta){
		PreviousState = MovementState;
		MovementState = MovementStates.Falling;
		Impulse = InImpulse;
		// Handle logic to swap between falling and sliding impulse
		Falling(delta);
		Impulse = Vector3.Zero;
	}

	public void Jump(Vector3 IntendedVector, float delta){
		
	}

	public void GroundedDash(Vector3 IntendedVector, float delta){

	}

	public void GroundedWalk(float delta){
		if(PreviousState == MovementStates.Falling && MovementState == MovementStates.GroundedWalk){
			PreviousState = MovementState;
			InheretedVelocity = Velocity;
			InheretedVelocity.y = 0;
		}
		else{
			if(InheretedVelocity.Length() > StopThreshold){
				InheretedVelocity -= InheretedVelocity.Normalized() * WalkFrictionScale * delta;
			}
			else{
				InheretedVelocity = Vector3.Zero;
			}
		}
		Func<float, float> deg2rad = (Degrees) => (float)(Degrees *Math.PI/180);
		Func<Vector3, float, Vector3> RotateByAngle = (Origin, Degrees) => (Origin.Rotated(Vector3.Up,deg2rad(Degrees)));
		Vector3 NormalForce = Vector3.Zero;
		if((RelativeVelocity.Length() <= StopThreshold) && (InputVector.Length() <= 0)){
			RelativeVelocity = Vector3.Zero;
		} 
		else {
			RelativeVelocity.x = (Math.Abs(RelativeVelocity.x) > StopThreshold) ? RelativeVelocity.x : 0;
			RelativeVelocity.x += (Math.Abs(InputVector.x) > StopThreshold) ? InputVector.x * WalkAccelerationScale * delta : -RelativeVelocity.Normalized().x * WalkFrictionScale * delta;
			
			RelativeVelocity.z = (Math.Abs(RelativeVelocity.z) > StopThreshold) ? RelativeVelocity.z : 0;
			RelativeVelocity.z += (Math.Abs(InputVector.z) > StopThreshold) ? InputVector.z * WalkAccelerationScale * delta : -RelativeVelocity.Normalized().z * WalkFrictionScale * delta;
			RelativeVelocity = (RelativeVelocity).LimitLength(MaxSpeed);
			if(IsOnWall()){
				// Affects total vel and not relative vel. Maybe not needed. Needs to affect inhereted vel.
				for(int i = 0; i < GetSlideCount(); i++){
					KinematicCollision CollisionInfo = GetSlideCollision(i);
					Vector3 RotatedNormal = RotateByAngle(CollisionInfo.Normal, ViewAngle.y);
					RotatedNormal = new Vector3(RotatedNormal.z, 0, RotatedNormal.x);
					RelativeVelocity += RotatedNormal * -RelativeVelocity.Dot(RotatedNormal);
				}
			}
		}
		Vector3 Rightward = Vector3.Right.Rotated(Vector3.Up,deg2rad(ViewAngle.y)) * RelativeVelocity.z;
		Vector3 Forward = Vector3.Forward.Rotated(Vector3.Up,deg2rad(ViewAngle.y)) * RelativeVelocity.x;
		Vector3 WalkVelocity = Forward + Rightward + InheretedVelocity + Vector3.Down;
		GD.Print(WalkVelocity);
		
		Velocity = MoveAndSlide(WalkVelocity,Vector3.Up);
		if(!IsOnFloor() && !IsOnWall()){
			PreviousState = MovementState;
			MovementState = MovementStates.Falling;
		}
		if(Input.IsActionJustPressed("jump")){
			SetImpulse(Vector3.Up*600,delta);
		}
	}

	public void Falling(float delta){
		Vector3 GravityForce = new Vector3(0,-FallAccel,0);
		Vector3 Friction = -Velocity.Normalized() * FrictionScale;
		NetForce = GravityForce + Friction + ExternalForce + Impulse;
		// Impulse is treated as an instantanious force and is set to zero after it is used.
		Impulse = Vector3.Zero;
		Velocity += NetForce * delta;
		Velocity = MoveAndSlide(Velocity,Vector3.Up);
		if(IsOnFloor()){
			PreviousState = MovementState;
			MovementState = MovementStates.GroundedWalk;
		}
		if(PreviousState == MovementStates.GroundedWalk && MovementState == MovementStates.Falling){
			RelativeVelocity = Vector3.Zero;
		}
	}

	public override void _Input(InputEvent Event){
		InputEventMouseMotion MouseEvent = Event as InputEventMouseMotion;
		if(MouseEvent != null){
			MouseDelta = MouseEvent.Relative;
		}
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		InheretedVelocity = Vector3.Zero;
		PropertyListChangedNotify();
		MovementState = MovementStates.GroundedWalk;
		PreviousState = MovementStates.GroundedWalk;
		ViewCamera = (Camera)GetNode("Camera");
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _PhysicsProcess(float delta){
		switch (MovementState){
			case MovementStates.GroundedWalk:
				GroundedWalk(delta);
				break;
			case MovementStates.Falling:
				Falling(delta);
				break;

			default:
				break;
		}
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
 public override void _Process(float delta)
 {
	// This possibly needs to be put into its own function
	ViewAngle.x -= MouseDelta.y * delta * MouseSensitivityY;
	ViewAngle.y -= MouseDelta.x * delta * MouseSensitivityX;
	ViewAngle.x = Mathf.Clamp(ViewAngle.x, -90, 90);
	ViewCamera.RotationDegrees = ViewAngle;
	MouseDelta = Vector2.Zero;

	InputVector = InputHandle.GetInputVector(
	Input.IsActionPressed("move_forward"),
	Input.IsActionPressed("move_back"),
	Input.IsActionPressed("move_left"),
	Input.IsActionPressed("move_right"));
 }
}
