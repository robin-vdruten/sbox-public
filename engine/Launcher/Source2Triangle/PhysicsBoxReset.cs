namespace Sandbox;

/// <summary>
/// Resets the physics box to a position above the scene when the player presses R.
/// Demonstrates keyboard input polling inside a <see cref="Component.OnUpdate"/> tick.
/// </summary>
internal sealed class PhysicsBoxReset : Component
{
	/// <summary>World-space position where the box is respawned.</summary>
	public Vector3 SpawnPosition { get; set; } = new Vector3( 80f, 0f, 300f );

	protected override void OnUpdate()
	{
		if ( Input.Keyboard.Pressed( "r" ) )
		{
			// Teleport back to the spawn point and zero velocity.
			WorldPosition = SpawnPosition;
			WorldRotation = Rotation.Identity;

			var body = GameObject.Components.Get<Rigidbody>();
			if ( body is not null )
			{
				body.Velocity = Vector3.Zero;
				body.AngularVelocity = Vector3.Zero;
			}
		}
	}
}
