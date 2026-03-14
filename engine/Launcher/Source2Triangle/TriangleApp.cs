using Sandbox.Engine;
using Sandbox.Internal;
using NativeEngine;
using System.Runtime.InteropServices;
using System;

namespace Sandbox;

/// <summary>
/// Bootstrap helper for the Source 2 Triangle Demo.
/// Initialises the Source 2 engine from scratch without inheriting from
/// <see cref="AppSystem"/> and renders a coloured triangle via the
/// engine's managed render-callback hooks.
/// </summary>
internal static class TriangleApp
{
	static CMaterialSystem2AppSystemDict _appSystem;

	/// <summary>
	/// Full application lifecycle: init, loop, shutdown.
	/// Called from <see cref="Launcher.Main"/> after
	/// <see cref="LauncherEnvironment.Init"/> has prepared paths.
	/// </summary>
	public static void Run()
	{
		try
		{
			Init();

			NativeEngine.EngineGlobal.Plat_SetCurrentFrame( 0 );

			bool wantsQuit = false;
			while ( !wantsQuit )
			{
				EngineLoop.RunFrame( _appSystem, out wantsQuit );

				// Keep the frame loop pumped during any blocking calls
				// (e.g. native file-open dialogs).
				BlockingLoopPumper.Run( () =>
				{
					EngineLoop.RunFrame( _appSystem, out _ );
				} );
			}

			Shutdown();
		}
		catch ( Exception e )
		{
			ErrorReporter.Initialize();
			ErrorReporter.ReportException( e );
			ErrorReporter.Flush();
			Console.WriteLine( $"Fatal error: ({e.GetType().Name}) {e.Message}" );
			NativeEngine.EngineGlobal.Plat_ExitProcess( 1 );
		}
	}

	// -------------------------------------------------------------------------
	// Initialisation
	// -------------------------------------------------------------------------

	static void Init()
	{
		Application.TryLoadVersionInfo( LauncherEnvironment.GamePath );

		// Load the interop bridge between C# and the native engine DLLs.
		NetCore.InitializeInterop( LauncherEnvironment.GamePath );

		// Fix command-line when run as a managed .dll host (.dll → .exe).
		var commandLine = Environment.CommandLine.Replace( ".dll", ".exe" );

		// Mark this process as a standalone game so Bootstrap skips
		// editor/tools paths and the Steam-inventory wait.
		Application.IsStandalone = true;

		// Create the GameInstance *before* Bootstrap.PreInit so that
		// Bootstrap can call IGameInstanceDll.Current.Bootstrap().
		GameInstanceDll.Create();

		// Build the engine app-system dictionary.
		// This is the object that owns the window, render device, etc.
		var createInfo = new MaterialSystem2AppSystemDictCreateInfo
		{
			iFlags = MaterialSystem2AppSystemDictFlags.IsGameApp
			       | MaterialSystem2AppSystemDictFlags.IsStandaloneGame,
			pWindowTitle = Marshal.StringToHGlobalAnsi( "Source 2 Triangle Demo" ),
		};

		_appSystem = CMaterialSystem2AppSystemDict.Create( createInfo );
		_appSystem.SetInStandaloneApp();
		_appSystem.SetSteamAppId( (uint)Application.AppId );

		if ( !NativeEngine.EngineGlobal.SourceEnginePreInit( commandLine, _appSystem ) )
			throw new Exception( "SourceEnginePreInit failed" );

		Bootstrap.PreInit( _appSystem );

		if ( !NativeEngine.EngineGlobal.SourceEngineInit( _appSystem ) )
			throw new Exception( "SourceEngineInit failed" );

		Bootstrap.Init();

		// The engine is fully up – create the scene that renders our triangle.
		TriangleScene.Setup();
	}

	// -------------------------------------------------------------------------
	// Shutdown
	// -------------------------------------------------------------------------

	static void Shutdown()
	{
		// Signal the game instance that we are closing.
		IGameInstanceDll.Current?.CloseGame();
		IGameInstanceDll.Current?.Exiting();

		// Flush any disposables that were queued for end-of-frame.
		EngineLoop.DrainFrameEndDisposables();
		MainThread.RunQueues();

		// Ask the native engine to shut down (closes the window, releases GPU resources, etc.)
		NativeEngine.EngineGlobal.SourceEngineShutdown( _appSystem, false );

		if ( _appSystem.IsValid )
		{
			_appSystem.Destroy();
			_appSystem = default;
		}

		Application.Shutdown();
	}
}
