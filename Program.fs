module Program
    open OpenTK.Graphics.OpenGL4
    open OpenTK.Windowing.Desktop
    open OpenTK.Mathematics
    open OpenTK.Windowing.Common
    open System
    open Microsoft.FSharp.Collections
    open Microsoft.FSharp.Control
    open System.Timers
    open GameAudio

    let gwSettings = GameWindowSettings()
    
    type ProgramMsg = 
        | Initialize
        | RenderFrame
        | Tick
        | KeyDown of key: string
        | KeyUp of key: string
        | CloseApp
        | Logic

    type Window(width, height, title) =
        inherit GameWindow(gwSettings, NativeWindowSettings(ClientSize=Vector2i(width, height), Title=title))

        override this.OnLoad (): unit = 
            GL.ClearColor(0.0f, 0.0f, 1.0f, 0.0f)
            base.OnLoad()
        
        override this.OnResize(e) =
            GL.Viewport(0, 0, this.ClientSize.X, this.ClientSize.Y)
            base.OnResize(e)

        override this.OnRenderFrame(e) =
            GL.Clear(ClearBufferMask.ColorBufferBit)
            this.Context.SwapBuffers()
            base.OnRenderFrame(e)

    type AppState = {KeyStates: Map<string, bool>; Window: Window option; AudioRef: AudioDevice option}

    let initialAppState = {
        KeyStates = Map<string, bool> [for x in 'a' .. 'z'  -> x.ToString(), false]
        Window = None
        AudioRef = None
        }

    [<EntryPoint>]
    let main args = 
        use window = new Window(640, 480, "Example Window")
        use timer = new Timer 1000;

        let printerAgent = MailboxProcessor.Start(fun inbox -> 
            let rec messageLoop(appState: AppState) = async {
                let! msg = inbox.Receive()

                match msg with 
                | Initialize ->
                    let audioDevice = createAudioDevice
                    if audioDevice <> None then
                        return! messageLoop {KeyStates = appState.KeyStates; Window = appState.Window; AudioRef = audioDevice}
                | RenderFrame -> ()
                | Tick -> printfn "Tick"; generateTone (appState.AudioRef, 180, 100)
                | KeyDown x -> printfn "Key is pressed: %s" x; return! messageLoop {KeyStates = appState.KeyStates.Add(x, true); Window = appState.Window; AudioRef = appState.AudioRef}
                | KeyUp x -> printfn "Key if released %s" x; return! messageLoop {KeyStates = appState.KeyStates.Add(x, false); Window = appState.Window; AudioRef = appState.AudioRef}
                | CloseApp -> window.Close(); timer.Stop()
                | Logic -> ()
                | _ -> printfn "received bad msg: %s" (msg.ToString())

                return! messageLoop appState
            }

            messageLoop initialAppState
        )

        let timerHandler = new ElapsedEventHandler(fun _ _ -> printerAgent.Post Tick)
        timerHandler |> timer.Elapsed.AddHandler

        printerAgent.Post Initialize
            
        let keyDownAction = Action<KeyboardKeyEventArgs> (fun e -> printerAgent.Post (KeyDown (e.Key.ToString())))
        let keyUpAction = Action<KeyboardKeyEventArgs> (fun e -> printerAgent.Post (KeyUp (e.Key.ToString())))
        keyDownAction |> window.add_KeyDown 
        keyUpAction |> window.add_KeyUp
        timer.Start()
        window.Run()
        0