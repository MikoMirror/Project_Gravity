using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] private float Speed = 5.0f;
	[Export] private float SprintSpeed = 8.0f;
	[Export] private float JumpVelocity = 4.5f;
	[Export] private float MouseSensitivity = 0.05f;
	[Export] private float WalkShakeAmount = 0.05f;
	[Export] private float WalkShakeSpeed = 15.0f;
	[Export] private float LandingShakeAmount = 0.2f;
	[Export] private float AirControl = 0.3f;
	[Export] private float GravityMultiplier = 1.5f;
	[Export] private float MinLiftDistance = 1.5f;
	[Export] private float MaxLiftDistance = 3.5f;
	[Export] private float LiftSpeed = 5.0f;
	[Export] private float MaxLiftWeight = 50.0f;
	[Export] private float ThrowForce = 5.0f;
	[Export] private float PushForce = 2.0f;
	[Export] private float RotationSpeed = 2.0f;
	[Export] private float GravityFlipDuration = 0.5f;
	[Export] private float RewindDuration = 1.0f; 

	public bool IsCameraInverted { get; set; } = false;

	// Player state
	public float VerticalRotation { get; set; } = 0f;
	public float HorizontalRotation { get; set; } = 0f;
	public float Gravity { get; set; }
	public bool WasInAir { get; set; } = false;
	public float ShakeTime { get; set; } = 0f;
	public float LandingShake { get; set; } = 0f;
	public bool IsSprinting { get; set; } = false;

	public RigidBody3D HeldObject { get; set; } = null;
	public bool IsLifting { get; set; } = false;
	public Vector3 LiftTarget { get; set; }

	// Gravity change state
	public bool IsGravityReversed { get; set; } = false;
	private float _gravityFlipTimer = 0f;
	private float _initialGravity;
	private bool _isFlipping = false;

	// Nodes
	private GravityManager _gravityManager;
	private PlayerUI _jumpsIndicator;
	private Node3D Head;
	private Camera3D Camera;
	private RayCast3D InteractionRay;
	private Marker3D HandPosition;
	private ColorRect CameraOverlay;
	private AnimationPlayer _animation;
	private Vector3 _initialPosition;
	private Quaternion _initialRotation;
	private ColorRect _teleportOverlay;
	private AnimationPlayer _animationPlayer;
	private Camera3D _camera;
	private Node3D _head;
	private bool _isTeleporting = false;
	private PlayerUI _playerUI;
	private ColorRect _fadeOverlay;
	private PlayerTeleporter _playerTeleporter;

	public override void _Ready()
	{
		// Get nodes and initialize
		_gravityManager = GetNode<GravityManager>("GravityManager");
		_jumpsIndicator = GetNode<PlayerUI>("PlayerUI");
		Head = GetNode<Node3D>("Head");
		Camera = GetNode<Camera3D>("Head/Camera3D");
			InteractionRay = Camera.GetNode<RayCast3D>("InteractionRay");
			HandPosition = Camera.GetNode<Marker3D>("HandPosition");
			CameraOverlay = GetNode<ColorRect>("Head/Camera3D/Flash");
			_animation = GetNode<AnimationPlayer>("AnimationPlayer");
			_teleportOverlay = GetNode<ColorRect>("Head/Camera3D/TeleportOverlay");
			_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
			_camera = GetNode<Camera3D>("Head/Camera3D");
			_head = GetNode<Node3D>("Head");
			_playerUI = GetNode<PlayerUI>("PlayerUI");
			_fadeOverlay = GetNode<ColorRect>("Head/Camera3D/FadeOverlay");
			_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

		if (_gravityManager == null)
		{
			GD.PrintErr("GravityManager not found! Make sure it's a child of the Player node.");
				_camera = GetNode<Camera3D>("Head/Camera3D");
				_head = GetNode<Node3D>("Head");
		}
		if (_jumpsIndicator == null)
		{
			GD.PrintErr("PlayerUI not found in Player!");
		}

		// Initialize settings
		Gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity") * GravityMultiplier;
		_initialGravity = Gravity;
		_initialPosition = GlobalPosition;
		_initialRotation = GlobalTransform.Basis.GetRotationQuaternion();

		// Configure nodes
		ConfigureInteractionRay();
		ConfigureInputMode();

		// Initialize PlayerTeleporter
		_playerTeleporter = new PlayerTeleporter(
			this,
			GetNode<AnimationPlayer>("AnimationPlayer"),
			GetNode<ColorRect>("Head/Camera3D/TeleportOverlay"),
			GetNode<Camera3D>("Head/Camera3D"),
			GetNode<Node3D>("Head")
		);
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleMovement(delta);
		HandleObjectLifting(delta);
		ApplyCameraShakes(delta);
		UpdateCameraRotation();
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
		CameraOverlay.Visible = true;
	}

	public void TriggerFlashEffect()
	{
		_animation.Play("flash_effect");
	}

	public void ToggleGravity()
	{
		if (_jumpsIndicator.CanJump())
		{
			IsGravityReversed = !IsGravityReversed;
			_isFlipping = true;
			_gravityFlipTimer = 0f;
			_gravityManager.UpdateHeldObjectGravity(IsGravityReversed);
			_jumpsIndicator.UseJump();
		}
	}

	public bool TryToggleGravity()
	{
		if (_jumpsIndicator.CanJump() && !_isFlipping)
		{
			IsGravityReversed = !IsGravityReversed;
			_isFlipping = true;
			_gravityFlipTimer = 0f;
			_gravityManager.UpdateHeldObjectGravity(IsGravityReversed);
			_jumpsIndicator.UseJump();
			return true;
		}
		return false;
	}

	public void Teleport(Vector3 targetPosition, Vector3? forwardDirection = null)
	{
		_playerTeleporter.StartTeleportAnimation(targetPosition, forwardDirection);
	}

	public void StartFadeOutAnimation()
	{
		if (_fadeOverlay != null && _animationPlayer != null)
		{
			_fadeOverlay.Visible = true;
			_animationPlayer.Play("fade_out");
			_animationPlayer.AnimationFinished += OnFadeOutFinished;
		}
		else
		{
			GD.PrintErr("Player: Unable to start fade out animation. FadeOverlay or AnimationPlayer is null.");
		}
	}

	private void OnFadeOutFinished(StringName animName)
	{
		if (animName == "fade_out")
		{
			_fadeOverlay.Visible = false;
			_animationPlayer.AnimationFinished -= OnFadeOutFinished;
		}
	}

	public override void _ExitTree()
	{
		GD.Print("Player _ExitTree called");
		if (IsInstanceValid(_playerUI) && _playerUI.IsInsideTree())
		{
			_playerUI.QueueFree();
		}
	}

	public void ResetGravityJumps()
	{
		if (_gravityManager != null)
		{
			_gravityManager.ResetGravityJumps();
		}
		if (_playerUI != null)
		{
			_playerUI.ResetJumps();
		}
	}
}
