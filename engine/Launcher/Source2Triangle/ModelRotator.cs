namespace Sandbox;

/// <summary>
/// Smoothly rotates the attached <see cref="GameObject"/> around its up axis.
/// Demonstrates a simple per-frame component update driven by <see cref="Time.Delta"/>.
/// </summary>
public sealed class ModelRotator : Component
{
	/// <summary>Degrees per second to rotate.</summary>
	public float DegreesPerSecond { get; set; } = 45f;

	protected override void OnUpdate()
	{
		WorldRotation = WorldRotation.RotateAroundAxis( Vector3.Up, DegreesPerSecond * Time.Delta );
	}
}
