using System;

namespace Sandbox;

internal static class TriangleScene
{
	static float TriangleRotation;
	static CameraComponent Camera;

	internal static void Setup()
	{
		var scene = new Scene();
		scene.Name = "Triangle Demo";

		Game.ActiveScene = scene;
		Game.IsPlaying = true;

		using var _ = scene.Push();

		var cameraGo = scene.CreateObject( true );
		cameraGo.Name = "Camera";

		Camera = cameraGo.AddComponent<CameraComponent>();

		Camera.BackgroundColor = new Color( 0.06f, 0.06f, 0.12f );
		Camera.FieldOfView = 70f;

		cameraGo.WorldPosition = new Vector3( 0f, -300f, 80f );
		cameraGo.WorldRotation = Rotation.LookAt( Vector3.Zero - cameraGo.WorldPosition );

		Camera.SceneCamera.OnRenderOverlay = DrawTriangle;
	}

	static void DrawTriangle()
	{
		TriangleRotation += Time.Delta * 90f;

		var rot = Rotation.FromYaw( TriangleRotation );

		float size = 120f;

		Vector3 p0 = rot * new Vector3( 0, 0, size );
		Vector3 p1 = rot * new Vector3( size, 0, -size );
		Vector3 p2 = rot * new Vector3( -size, 0, -size );

		// Convert world → screen
		Vector3 s0 = Camera.SceneCamera.ToScreen( p0 );
		Vector3 s1 = Camera.SceneCamera.ToScreen( p1 );
		Vector3 s2 = Camera.SceneCamera.ToScreen( p2 );

		Vertex[] verts =
		[
			new Vertex(new Vector3(s0.x, s0.y, 0), new Color32(255,50,50)),
			new Vertex(new Vector3(s1.x, s1.y, 0), new Color32(50,255,50)),
			new Vertex(new Vector3(s2.x, s2.y, 0), new Color32(50,100,255)),
		];

		Graphics.Draw( verts, 3, Material.UI.Basic );

		float fps = 1f / Time.Delta;

		Graphics.DrawText(
			new Rect( 10, 10, 300, 40 ),
			$"FPS: {fps:F0}",
			Color.White,
			fontSize: 22
		);
	}
}
