using Sandbox.Engine;
using Sandbox.Engine.Settings;
using Sandbox.Internal;
using Sandbox.Diagnostics;
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

	[DllImport( "kernel32.dll", SetLastError = true )]
	static extern bool AllocConsole();

	[DllImport( "kernel32.dll", SetLastError = true )]
	static extern bool FreeConsole();

	static void Init()
	{
		AllocConsole();
		Application.TryLoadVersionInfo( LauncherEnvironment.GamePath );

		// Load the interop bridge between C# and the native engine DLLs.
		NetCore.InitializeInterop( LauncherEnvironment.GamePath );

		// Fix command-line when run as a managed .dll host (.dll → .exe).
		// Append -novid so the native engine skips its intro/splash video.
		var commandLine = Environment.CommandLine.Replace( ".dll", ".exe" );
		if ( !commandLine.Contains( "-nosplash" ) )
			commandLine += " -nosplash";

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
			pWindowTitle = Marshal.StringToHGlobalAnsi( "Source 2 Game Demo" ),
		};

		_appSystem = CMaterialSystem2AppSystemDict.Create( createInfo );
		_appSystem.SetInStandaloneApp();
		_appSystem.SetSteamAppId( (uint)Application.AppId );

		if ( !NativeEngine.EngineGlobal.SourceEnginePreInit( commandLine, _appSystem ) )
			throw new Exception( "SourceEnginePreInit failed" );

		unsafe
		{
			IntPtr addr = (IntPtr)NativeEngine.EngineGlobal.__N.global_SourceEnginePreInit;
			Console.WriteLine( $"SourceEnginePreInit address: 0x{addr.ToString( "X" )}" );
		}

		Bootstrap.PreInit( _appSystem );

		Standalone.Init();

		// Ensure all engine errors and warnings are forwarded to the system console
		// (Bootstrap.PreInit sets PrintToConsole = false for non-headless apps by default).
		Logging.PrintToConsole = true;

		if ( !NativeEngine.EngineGlobal.SourceEngineInit( _appSystem ) )
			throw new Exception( "SourceEngineInit failed" );

		Bootstrap.Init();

		// Switch to windowed mode – not fullscreen, not borderless.
		var video = RenderSettings.Instance;
		video.Fullscreen = false;
		video.Borderless = false;
		video.ResolutionHeight = 780;
		video.ResolutionWidth = 1280;
		video.Apply();

		// The engine is fully up – create the scene that renders our model demo.
		GameScene.Setup();
		//TriangleScene.Setup();
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
