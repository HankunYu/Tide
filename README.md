# Tide

A deterministic timeline for Unity. Every frame depends on the time alone:
seeking, scrubbing and playback show the same thing, and a capture repeats
exactly, take after take. Like a tide table, anything that builds up over
time is worked out ahead, so any moment can be looked up directly.

## Features

- **Clip tracks** with ease in/out, crossfades between overlapping clips and
  pre/post extrapolation (hold, loop).
- **Built-in tracks:** channel (any value a component exposes), activation,
  transform, animation (Animator clips, crossfaded, in edit mode too), audio,
  markers and nested sub-timelines.
- **Typed channels:** float, int, bool, colour (HDR), vectors and gradients.
  Continuous values interpolate across eases and crossfades; discrete ones
  (int, bool, gradient) switch halfway. During playback a channel is only
  written when its value changes, so a clip holding a value costs nothing
  (*Skip Unchanged Channels* on the player); seeks always write.
- **Material channels:** `TideMaterialChannels` exposes every parameter a
  renderer's shader shows in the inspector, written through a
  MaterialPropertyBlock so the material asset is never touched.
- **Visual effects** (when the VFX Graph package is installed, in the
  `Tide.VFX` assembly): `TideVisualEffectChannels` exposes a graph's exposed
  properties, and the visual effect track plays an effect during its clips.
  Seeking into a clip restarts the effect and simulates it up to that moment,
  and edit mode steps the simulation, so scrubbing shows the particles.
- **Phase integration:** components whose effects accumulate at an adjustable
  rate (a speed you can animate) implement `ITidePhaseTarget`; the player
  integrates their rates over the whole timeline into a table when it binds,
  so the phase at any time is a lookup instead of a running sum.
- **Scene clock:** `TideClock.Time` is the timeline's time while a player that
  drives the clock is bound, and game or editor time otherwise. Read it
  instead of `Time.time` and your effects follow the timeline too.
- **Edit-mode preview** without play mode: binding remembers the targets'
  values and unbinding puts them back, so previewing never dirties the scene.
- **Audio clock:** the player can follow an `AudioSource`, for music-driven pieces.
- **No dependencies**: the Unity Timeline package is not needed.

## Install

Reference the repository from `Packages/manifest.json`:

```json
"com.hankun.tide": "https://github.com/HankunYu/Tide.git"
```

Append `#<tag or commit>` to pin a version. To change Tide while working on
a project, add the repository as a git submodule under `Packages/` instead;
Unity then treats it as an editable embedded package.

## Quick start

1. *Create > Tide > Timeline* makes a timeline asset.
2. Add a **Tide Player** to a GameObject (*Add Component > Tide > Tide Player*)
   and assign the asset.
3. *Window > Tide* opens the editor. Drag scene objects onto it to add tracks
   bound to them, add clips, and scrub to preview in edit mode.

The asset never references the scene: each player keeps its own binding per
track, so one timeline can be played by several players.

## Extending

- **Channels:** implement `ITideChannelTarget` on a component to expose named,
  typed channels (`TideValue`) to channel tracks.
- **Tracks and clips:** subclass `TideClipTrack` (or `TideTrack`) and `TideClip`.
  The editor finds new types on its own. `[TideDisplay("Name", r, g, b)]`
  gives a track its name and colour in the editor.
- **Clip tools:** subclass `TideClipEditor` in an editor assembly to add
  buttons for a clip type ("fit to content", "capture from the scene") and to
  fill new clips from what the track is bound to.
- **Phases:** implement `ITidePhaseTarget` to have the player own a
  component's accumulated phases while it is bound.

## License

MIT — see [LICENSE.md](LICENSE.md).
