namespace Sandbox;

/// <summary>
/// Orbit camera controller. Attach to a <see cref="CameraComponent"/> GameObject.
/// <list type="bullet">
///   <item>Hold <b>right mouse button</b> and drag to orbit around the focus point.</item>
///   <item>Use the <b>scroll wheel</b> to dolly in / out.</item>
/// </list>
/// </summary>
public sealed class OrbitCameraController : Component, Sandbox.Internal.IUpdateSubscriber
{
	/// <summary>Distance from the orbit pivot to the camera.</summary>
	public float Distance { get; set; } = 300f;

	/// <summary>Horizontal sensitivity (degrees / pixel).</summary>
	public float YawSensitivity { get; set; } = 0.3f;

	/// <summary>Vertical sensitivity (degrees / pixel).</summary>
	public float PitchSensitivity { get; set; } = 0.3f;

	/// <summary>Scroll-wheel zoom speed.</summary>
	public float ZoomSpeed { get; set; } = 15f;

	/// <summary>Minimum pitch angle (degrees, negative = below horizon).</summary>
	public float MinPitch { get; set; } = -80f;

	/// <summary>Maximum pitch angle (degrees).</summary>
	public float MaxPitch { get; set; } = 80f;

	/// <summary>The world-space point the camera orbits around.</summary>
	public Vector3 Pivot { get; set; } = Vector3.Zero;

	float _yaw = 180f;
	float _pitch = 20f;

	protected override void OnStart()
	{
		// Initialise yaw / pitch from the current transform so the camera
		// doesn't snap on the first frame.
		var dir = Pivot - WorldPosition;
		if ( dir.LengthSquared > 0.01f )
		{
			var angles = Rotation.LookAt( dir ).Angles();
			_yaw = angles.yaw;
			_pitch = -angles.pitch;
			Distance = dir.Length;
		}
	}

	protected override void OnUpdate()
	{
		// Orbit only while the right mouse button is held.
		if ( Input.Down( "attack2" ) )
		{
			_yaw += Input.MouseDelta.x * YawSensitivity;
			_pitch = System.Math.Clamp( _pitch + Input.MouseDelta.y * PitchSensitivity, MinPitch, MaxPitch );
		}
		// Scroll wheel zooms (dolly in/out).
		Distance -= Input.MouseWheel.y * ZoomSpeed;
		Distance = System.Math.Max( 50f, Distance );

		// Recompute camera position from spherical coordinates.
		var rot = Rotation.From( _pitch, _yaw, 0f );
		WorldPosition = Pivot - rot.Forward * Distance;
		WorldRotation = rot;
	}
}
