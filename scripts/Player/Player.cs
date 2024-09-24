using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody3D
{
	#region Exported Variables
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
	#endregion

	#region Player State
	public bool IsCameraInverted { get; set; } = false;
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
	#endregion

	#region Gravity Change State
	public bool IsGravityReversed { get; set; } = false;
	private float _gravityFlipTimer = 0f;
	private float _initialGravity;
	private bool _isFlipping = false;
	#endregion

	#region Nodes
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
	#endregion

	#region Godot Methods
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
			_camera = GetNode<Camera3D>("Head/Camera3D");
			_head = GetNode<Node3D>("Head");
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
		}

		_deadOverlay = GetNode<ColorRect>("Head/Camera3D/Dead");
		if (_deadOverlay != null)
		{
			_deadOverlay.Visible = false;
		}

		ResetState(); 
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleMovement(delta);
		HandleObjectLifting(delta);
		ApplyCameraShakes(delta);
		UpdateCameraRotation();
		HandleFootsteps(delta);
	}
	#endregion

	#region Initialization Methods
	private void InitializeTeleporter()
	{
		if (_animationPlayer == null)
		{
			return;
		}
		if (_teleportOverlay == null)
		{
			return;
		}
		if (_camera == null)
		{
			return;
		}
		if (_head == null)
		{
			return;
		}

		_teleporter = new PlayerTeleporter(this, _animationPlayer, _teleportOverlay, _camera, _head);
		GD.Print("Player: PlayerTeleporter initialized successfully.");
	}
	#endregion

	#region Configuration Methods
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
	#endregion

	#region Player Actions
	public void TriggerFlashEffect()
	{
		_animation.Play("flash_effect");
	}

	public bool ToggleGravity()
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
			InitializeTeleporter();
			if (_teleporter == null)
			{
				return;
			}
		}
		_teleporter.StartTeleportAnimation(targetPosition, forwardDirection);
	}

	public void FinishTeleportation()
	{
		if (_teleporter == null)
		{
			return;
		}
		_teleporter.FinishTeleportAnimation();
	}

	public event Action TeleportCompleted
	{
		add 
		{ 
			if (_teleporter != null) _teleporter.TeleportCompleted += value; 
		}
		remove 
		{ 
			if (_teleporter != null) _teleporter.TeleportCompleted -= value; 
		}
	}

	public void StartSpawnAnimation()
	{
		if (_fadeOverlay == null || _animationPlayer == null)
		{
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
	#endregion

	#region Footsteps
	private const float WALK_STEP_INTERVAL = 0.45f;
	private const float RUN_STEP_INTERVAL = 0.3f;
	private float _currentStepInterval = WALK_STEP_INTERVAL;
	#endregion

	#region Death and Respawn
	public void Die()
	{
		if (_isDead) return; 

		_isDead = true;

		if (_deadOverlay != null)
		{
			_deadOverlay.Visible = true;
		}

		if (_animationPlayer != null && _animationPlayer.HasAnimation("dead_animation"))
		{
			_animationPlayer.Play("dead_animation");
			GetTree().CreateTimer(DEATH_ANIMATION_DURATION).Timeout += OnDeathAnimationFinished;
		}
		else
		{
			OnDeathAnimationFinished();
		}
	}

	private void OnDeathAnimationFinished()
	{
		GetTree().CreateTimer(DEATH_FREEZE_DURATION).Timeout += OnRestartTimer;
	}

	private void OnRestartTimer()
	{
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
	#endregion
}
