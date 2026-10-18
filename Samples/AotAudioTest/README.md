# AotAudioTest

Smallest code-only FlatRedBall game for checking audio, in particular that audio follows the
Windows default output device (`DefaultAudioDeviceFollower`) in a normal build and under Native AOT.

- Space plays a short sound effect (`AudioManager.Play`)
- M starts and stops a looping song (`AudioManager.PlaySong`)

The window title lists the keys. Every audio call is logged to `aotaudiotest.log` next to the exe,
including failures. `--autotest` presses the keys on a timer and exits, for checking a build
without a human.

## Running it

```
dotnet run --project Samples/AotAudioTest/AotAudioTest.csproj
```

Native AOT needs `vswhere.exe` on PATH for the linker step:

```
export PATH="$PATH:/c/Program Files (x86)/Microsoft Visual Studio/Installer"
dotnet publish Samples/AotAudioTest/AotAudioTest.csproj -r win-x64 -p:PublishAot=true
```

The exe lands in `Samples/AotAudioTest/bin/Release/net8.0/win-x64/publish/`.

## Notes

- Sounds are plain `.wav` files loaded with `SoundEffect.FromStream`, so there is no content
  pipeline. `Content/Shader.xnb` is the one `.xnb`; the engine loads it at startup.
- The song is a `.wav` played through `WavSong` (an `ISong`) because MonoGame's DesktopGL `Song`
  only plays `.ogg`.
