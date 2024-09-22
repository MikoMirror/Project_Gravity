using Godot;
using System;
using System.Collections.Generic;

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
	private PlayerTeleporter _teleporter;
	

	private SoundManager _soundManager;
	private List<string> _footstepSoundPaths = new List<string>();

	private AudioStreamPlayer3D _footstepPlayer;
	private List<AudioStream> _footstepSounds = new List<AudioStream>();
	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	private float _timeSinceLastStep = 0f;
	private const float STEP_INTERVAL = 0.2f; // Adjust this value to change how often footsteps play

	private bool _isDead = false;
	private ColorRect _deadOverlay;
	private const float DEATH_ANIMATION_DURATION = 0.2f;
	private const float DEATH_FREEZE_DURATION = 2.0f;

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
		InitializeTeleporter();

		_soundManager = GetNode<SoundManager>("/root/SoundManager");

		// Load all footstep sound paths
		for (int i = 1; i <= 10; i++)
		{
			_footstepSoundPaths.Add($"res://assets/Sounds/footsteps/{i}.mp3");
		}

		_footstepPlayer = GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");

		// Load all footstep sounds
		for (int i = 1; i <= 10; i++)
		{
			var sound = GD.Load<AudioStream>($"res://assets/Sounds/footsteps/{i}.mp3");
			_footstepSounds.Add(sound);
		}

		_rng.Randomize();

		if (_animationPlayer == null)
		{
			_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
			if (_animationPlayer == null)
			{
				GD.PrintErr("Player: AnimationPlayer not found!");
			}
		}

		_deadOverlay = GetNode<ColorRect>("Head/Camera3D/Dead");
		if (_deadOverlay == null)
		{
			GD.PrintErr("Player: Dead overlay ColorRect not found!");
		}
		else
		{
			_deadOverlay.Visible = false;
		}

		ResetState(); // Reset the player state when the scene loads
	}

	private void InitializeTeleporter()
	{
		if (_animationPlayer == null)
		{
			GD.PrintErr("Player: AnimationPlayer not found!");
			return;
		}
		if (_teleportOverlay == null)
		{
			GD.PrintErr("Player: TeleportOverlay not found!");
			return;
		}
		if (_camera == null)
		{
			GD.PrintErr("Player: Camera3D not found!");
			return;
		}
		if (_head == null)
		{
			GD.PrintErr("Player: Head node not found!");
			return;
		}

		_teleporter = new PlayerTeleporter(this, _animationPlayer, _teleportOverlay, _camera, _head);
		GD.Print("Player: PlayerTeleporter initialized successfully.");
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleMovement(delta);
		HandleObjectLifting(delta);
		ApplyCameraShakes(delta);
		UpdateCameraRotation();
		HandleFootsteps(delta);
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

	public void Teleport(Vector3 targetPosition, Vector3 forwardDirection)
	{
		if (_teleporter == null)
		{
			GD.PrintErr("Player: _teleporter is null. Attempting to reinitialize.");
			InitializeTeleporter();
			if (_teleporter == null)
			{
				GD.PrintErr("Player: Failed to initialize _teleporter. Teleportation aborted.");
				return;
			}
		}
		_teleporter.StartTeleportAnimation(targetPosition, forwardDirection);
	}

	public void FinishTeleportation()
	{
		if (_teleporter == null)
		{
			GD.PrintErr("Player: _teleporter is null. FinishTeleportation aborted.");
			return;
		}
		_teleporter.FinishTeleportAnimation();
	}

	public event Action TeleportCompleted
	{
		add 
		{ 
			if (_teleporter != null) _teleporter.TeleportCompleted += value; 
			else GD.PrintErr("Player: Cannot add TeleportCompleted event. _teleporter is null.");
		}
		remove 
		{ 
			if (_teleporter != null) _teleporter.TeleportCompleted -= value; 
			else GD.PrintErr("Player: Cannot remove TeleportCompleted event. _teleporter is null.");
		}
	}

	public void StartSpawnAnimation()
	{
		if (_fadeOverlay == null || _animationPlayer == null)
		{
			GD.PrintErr("Player: Unable to start spawn animation. FadeOverlay or AnimationPlayer is missing.");
			return;
		}

		_fadeOverlay.Visible = true;
		if (_animationPlayer.HasAnimation("fade_out"))
		{
			_animationPlayer.Play("fade_out");
			_animationPlayer.AnimationFinished += OnSpawnAnimationFinished;
		}
		else
		{
			GD.PrintErr("Player: 'fade_out' animation not found.");
			_fadeOverlay.Visible = false;
		}
	}

	private void OnSpawnAnimationFinished(StringName animName)
	{
		if (animName == "fade_out" && _fadeOverlay != null)
		{
			_fadeOverlay.Visible = false;
		}
		if (_animationPlayer != null)
		{
			_animationPlayer.AnimationFinished -= OnSpawnAnimationFinished;
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

	private void HandleFootsteps(double delta)
	{
		if (IsOnFloor() && Velocity.LengthSquared() > 0.1f)
		{
			_currentStepInterval = IsSprinting ? RUN_STEP_INTERVAL : WALK_STEP_INTERVAL;
			_timeSinceLastStep += (float)delta;
			if (_timeSinceLastStep >= _currentStepInterval)
			{
				PlayRandomFootstep();
				_timeSinceLastStep = 0f;
			}
		}
		else
		{
			_timeSinceLastStep = _currentStepInterval; // Reset timer when not moving
		}
	}

	private void PlayRandomFootstep()
	{
		int randomIndex = _rng.RandiRange(0, _footstepSoundPaths.Count - 1);
		_soundManager.PlaySound(_footstepSoundPaths[randomIndex]);
	}

	private const float WALK_STEP_INTERVAL = 0.45f;
	private const float RUN_STEP_INTERVAL = 0.3f;
	private float _currentStepInterval = WALK_STEP_INTERVAL;

	public void Die()
	{
		if (_isDead) return; // Prevent multiple deaths

		_isDead = true;
		GD.Print("Player: Die() called");

		if (_deadOverlay != null)
		{
			_deadOverlay.Visible = true;
		}

		if (_animationPlayer != null && _animationPlayer.HasAnimation("dead_animation"))
		{
			GD.Print("Player: Playing dead_animation");
			_animationPlayer.Play("dead_animation");
			GetTree().CreateTimer(DEATH_ANIMATION_DURATION).Timeout += OnDeathAnimationFinished;
		}
		else
		{
			GD.PrintErr("Player: dead_animation not found or AnimationPlayer is null");
			OnDeathAnimationFinished();
		}
	}

	private void OnDeathAnimationFinished()
	{
		GD.Print("Player: Death animation finished, starting freeze");
		GetTree().CreateTimer(DEATH_FREEZE_DURATION).Timeout += OnRestartTimer;
	}

	private void OnRestartTimer()
	{
		GD.Print("Player: OnRestartTimer called");
		var levelManager = GetTree().CurrentScene.GetNodeOrNull<LevelManager>("LevelManager");
		if (levelManager != null)
		{
			GD.Print("Player: Calling RestartLevel on LevelManager");
			levelManager.RestartLevel();
		}
		else
		{
			GD.Print("Player: LevelManager not found, restarting level directly");
			RestartLevelDirectly();
		}
	}

	private void RestartLevelDirectly()
	{
		var currentScene = GetTree().CurrentScene;
		if (currentScene != null)
		{
			string currentScenePath = currentScene.SceneFilePath;
			GD.Print($"Player: Restarting level: {currentScenePath}");
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, currentScenePath);
		}
		else
		{
			GD.PrintErr("Player: Unable to restart level, current scene is null");
		}
	}

	public void ResetState()
	{
		_isDead = false;
		if (_deadOverlay != null)
		{
			_deadOverlay.Visible = false;
		}
		if (_animationPlayer != null)
		{
			_animationPlayer.Stop(); 
			if (_animationPlayer.HasAnimation("RESET"))
			{
				_animationPlayer.Play("RESET");
			}
		}
	}
}
