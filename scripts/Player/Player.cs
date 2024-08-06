using Godot;
using System;

public partial class Player : CharacterBody3D
{
	private GravityManager _gravityManager;

	// Constants
	public const float Speed = 5.0f;
	public const float SprintSpeed = 8.0f;
	public const float JumpVelocity = 4.5f;
	public const float MouseSensitivity = 0.002f;
	public const float WalkShakeAmount = 0.05f;
	public const float WalkShakeSpeed = 15.0f;
	public const float LandingShakeAmount = 0.2f;
	public const float AirControl = 0.3f;
	public const float GravityMultiplier = 1.5f;
	public const float MinLiftDistance = 1.5f;
	public const float MaxLiftDistance = 2.5f;
	public const float LiftSpeed = 5.0f;
	public const float MaxLiftWeight = 50.0f;
	public const float ThrowForce = 5.0f;
	public const float PushForce = 2.0f;
	public const float RotationSpeed = 2.0f;
	public const float GravityFlipDuration = 0.5f;
	public bool IsCameraInverted { get; set; } = false;

	// Player state
	public float VerticalRotation = 0f;
	public float HorizontalRotation = 0f;
	public float Gravity;
	public bool WasInAir = false;
	public float ShakeTime = 0f;
	public float LandingShake = 0f;
	public bool IsSprinting = false;

	// Object lifting state
	public RigidBody3D HeldObject = null;
	public bool IsLifting = false;
	public Vector3 LiftTarget;

	// Gravity change state
	public bool IsGravityReversed = false;
	private float _gravityFlipTimer = 0f;
	private float _initialGravity;
	private bool _isFlipping = false;

	// Nodes
	public Node3D Head;
	public Camera3D Camera;
	public RayCast3D InteractionRay;
	public Marker3D HandPosition;
	public ColorRect CameraOverlay;
	public Vector3 InitialCameraPosition;

	  public override void _Ready()
	{
		InitializeNodes();
		InitializeSettings();
		_gravityManager = GetNode<GravityManager>("GravityManager");
		if (_gravityManager == null)
		{
			GD.PrintErr("GravityManager not found! Make sure it's a child of the Player node.");
		}
	}


	public override void _PhysicsProcess(double delta)
{
	HandleMovement(delta);
	HandleObjectLifting(delta);
	ApplyCameraShakes(delta);
	UpdateCameraRotation();
}

	private void InitializeNodes()
	{
		Head = GetNode<Node3D>("Head");
		Camera = GetNode<Camera3D>("Head/Camera3D");
		InteractionRay = Camera.GetNode<RayCast3D>("InteractionRay");
		HandPosition = Camera.GetNode<Marker3D>("HandPosition");
		InitialCameraPosition = Camera.Position;
		CameraOverlay = GetNode<ColorRect>("Head/Camera3D/MeshInstance3D/cameraPost");

		ConfigureInteractionRay();
		ConfigureInputMode();
	}

	private void ConfigureInteractionRay()
	{
		InteractionRay.Enabled = true;
		InteractionRay.TargetPosition = -Camera.GlobalTransform.Basis.Z * MaxLiftDistance;
		InteractionRay.CollideWithAreas = false;
		InteractionRay.CollideWithBodies = true;
		InteractionRay.CollisionMask = 1;
	}

	private void ConfigureInputMode()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		CameraOverlay.Visible = false;
	}

	private void InitializeSettings()
	{
		Gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity") * GravityMultiplier;
		_initialGravity = Gravity;
	}
}
