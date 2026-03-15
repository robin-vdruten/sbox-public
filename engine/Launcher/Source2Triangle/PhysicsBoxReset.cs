using System;

namespace Sandbox;

/// <summary>
/// Resets the physics box to a position above the scene when the player presses R.
/// Demonstrates keyboard input polling inside a <see cref="Component.OnUpdate"/> tick.
/// </summary>
public sealed class PhysicsBoxReset : Component, Sandbox.Internal.IUpdateSubscriber
{
	/// <summary>World-space position where the box is respawned.</summary>
	public Vector3 SpawnPosition { get; set; } = new Vector3( 80f, 0f, 300f );

	protected override void OnStart()
	{
		Console.WriteLine( "Press R to reset the physics box to its spawn point." );
	}

	protected override void OnUpdate()
	{
		if ( Input.Keyboard.Pressed( "r" ) )
		{
			WorldPosition = SpawnPosition;
			WorldRotation = Rotation.Identity;

			var body = GameObject.Components.Get<Rigidbody>();
			if ( body is not null )
			{
				body.Velocity = Vector3.Zero;
				body.AngularVelocity = Vector3.Zero;
				body.ApplyForce( Vector3.Zero );
			}
		}
	}
}
