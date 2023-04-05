using Godot;
using System;
using HandleInput;

public class Player : KinematicBody
{
	[Export]
	public float MaxSpeed = 300;
	[Export]
	public float Acceleration = 1500;
	[Export]
	public float Friction = 6.5f;
	[Export]
	public float MouseSensitivityX = 5;
	[Export]
	public float MouseSensitivityY = 5;
	[Export]
	public float FallAccel = 0.5f;
	[Export]
	public float MaxFallSpeed = 60f;
	private KinematicCollision CollisionInfo = new KinematicCollision();
	private float FallSpeed = 0;

	private Camera ViewCamera = new Camera();

	private Vector2 MouseDelta = new Vector2();

	InputHandler InputHandle = new InputHandler();
	private Vector3 InputVector = Vector3.Zero;
	private Vector3 ViewAngle = new Vector3();

	private Vector3 RelativeVelocity = new Vector3();
	private Vector3 Velocity = new Vector3();

	private MovementStates MovementState;
	private enum MovementStates{
		GroundedWalk,
		Falling,
	}
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Movement functions

	public void Jump(Vector3 IntendedVector, float delta){
		
	}

	public void GroundedDash(Vector3 IntendedVector, float delta){

	}

	public void GroundedWalk(Vector3 IntendedVector, float delta){
		Func<float, float> deg2rad = (Degrees) => (float)(Degrees *Math.PI/180);
		Func<Vector3, float, Vector3> RotateByAngle = (Origin, Degrees) => (Origin.Rotated(Vector3.Up,deg2rad(Degrees)));
		Vector3 NormalForce = Vector3.Zero;
		if((RelativeVelocity.Length() <= 0.1f) && (IntendedVector.Length() <= 0)){
			RelativeVelocity = Vector3.Zero;
		} 
		else {
			RelativeVelocity.x = (Math.Abs(RelativeVelocity.x) > 0.1f) ? RelativeVelocity.x : 0;
			RelativeVelocity.x += (Math.Abs(IntendedVector.x) > 0.1f) ? IntendedVector.x * Acceleration * delta : -RelativeVelocity.x * Friction * delta;
			
			RelativeVelocity.z = (Math.Abs(RelativeVelocity.z) > 0.1f) ? RelativeVelocity.z : 0;
			RelativeVelocity.z += (Math.Abs(IntendedVector.z) > 0.1f) ? IntendedVector.z * Acceleration * delta : -RelativeVelocity.z * Friction * delta;
			RelativeVelocity = (RelativeVelocity).LimitLength(MaxSpeed);
			if(IsOnWall()){
				// Affects total vel and not relative vel. Maybe not needed.
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

		
		Velocity = (Forward + Rightward) * delta;
		Velocity.y = -1;
		Velocity = MoveAndSlide(Velocity,Vector3.Up);
		if(!IsOnFloor() && !IsOnWall()){
			MovementState = MovementStates.Falling;
		}
	}

	public void Falling(Vector3 IntendedVector, float delta){
		if(IsOnFloor()){
			FallSpeed = 0;
			MovementState = MovementStates.GroundedWalk;
		} else{
			FallSpeed += FallAccel;
			Velocity.y = -FallSpeed;
			Velocity = Velocity.LimitLength(MaxFallSpeed);
			Velocity = MoveAndSlide(Velocity,Vector3.Up);
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
		PropertyListChangedNotify();
		MovementState = MovementStates.GroundedWalk;
		ViewCamera = (Camera)GetNode("Camera");
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _PhysicsProcess(float delta){
		// GD.Print(MovementState);
		switch (MovementState){
			case MovementStates.GroundedWalk:
				GroundedWalk(InputVector, delta);
				break;
			case MovementStates.Falling:
				Falling(InputVector,delta);
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
