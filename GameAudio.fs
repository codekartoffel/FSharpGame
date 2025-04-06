module GameAudio
    open OpenTK.Audio
    open OpenTK.Audio.OpenAL
    open System
    type AudioDevice = {Device: ALDevice; Context: ALContext}

    let createAudioDevice: AudioDevice option = 
        try
            let device = ALC.OpenDevice null 
            let attributes = new ALContextAttributes()
            let context = ALC.CreateContext(device, attributes)
            ALC.MakeContextCurrent context |> ignore

            Some {Device = device; Context = context}
        with e ->
            printfn "Could not create audio device: %s" e.Message
            None

    let printInfo =
        printfn "Audio (OpenAL) (Version: %s)" (AL.Get ALGetString.Version)

    let private generateWave (freq: float, duration: float) =
        let sampleFreq = 44100.
        let freq = 440.
        let output = List.init (sampleFreq * duration |> int) (fun i -> 
                                            2.0 * Math.PI * freq / sampleFreq * float i
                                            |> Math.Sin
                                            |> ( * )  (float Int16.MaxValue)
                                            |> int16
                                            )
        output |> Array.ofList

    let generateTone (device: AudioDevice option, toneFreq: float, duration: float) =
        if device <> None then
            let mutable source: int = 0
            let mutable buffer: int = 0

            AL.GenBuffers(1, &buffer)
            AL.GenSources(1, &source)

            let waveData = generateWave (toneFreq, duration) // C-4 (261.6, 0.5)
            
            AL.BufferData(buffer, ALFormat.Mono16 ,&waveData[0], waveData.Length * 2, int 44100)
            AL.Source(source, ALSourcei.Buffer, buffer)
            AL.SourcePlay source

            let error = AL.GetError()
            if error <> ALError.NoError then
                printfn "Audio error: %s" (error.ToString())

            AL.DeleteBuffer buffer
            AL.DeleteSource source


    let disposeAudioDevice (device: AudioDevice option) =
        if device <> None then
            ALC.MakeContextCurrent(ALContext.Null) |> ignore
            ALC.DestroyContext device.Value.Context
            ALC.CloseDevice device.Value.Device |> ignore