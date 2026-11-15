# Sliced Fill

**Created by blumgames · Version 1.0.1**

Reveal a nine-sliced sprite with the fill settings of a standard Unity UI Image.
No replacement Image, custom shader, or manual component setup required.

## Quick start

1. Import `Blumgames.SlicedFill-1.0.1.unitypackage` into your project.
2. Select a standard **UI → Image** and assign a sprite with **Border** values.
   Set the borders in **Sprite Editor → Border → Apply** if needed.
3. Set **Image Type → Filled**.
4. Enable **Sliced Fill** in the same Image inspector.
5. Adjust **Fill Amount**, **Fill Method**, and **Fill Origin** as usual.

When enabled, **Fill Center** and **Pixels Per Unit Multiplier** appear under the
checkbox. Higher multiplier values make borders thinner. The author credit is
only shown in the editor, never in your game's UI.

Drag `Samples/Rounded Progress Bar.prefab` into a Canvas for a ready-made example.
It uses Unity's built-in UI sprite and has no dependencies on game artwork.

## Behavior

- Horizontal / Vertical: reveal a portion of the full-size nine-sliced image,
  just like standard Filled. The fill edge crops the sprite and interpolates UVs;
  it never pulls the opposite end cap inward or squeezes the artwork. All origins
  are supported. Borders only compress if the full Image itself is too small.
- Radial 90 / 180 / 360: reveals a sector of the nine-sliced image. Radial cut edges
  are straight; outer sprite corners keep their original slicing.
- No sprite or no Border: standard Unity fill is used.
- Switching to another Image Type temporarily bypasses the effect. The checkbox
  setting is remembered when you switch back to Filled.
- Turning the checkbox off immediately restores standard Filled rendering.
- Preview works in Edit Mode and Play Mode. Undo / Redo, prefabs and multi-object
  editing are supported.

## Existing code and animations

Continue using `UnityEngine.UI.Image.fillAmount`, Animator, or your existing tween.
Image references, materials, Canvas masking and layout stay in place.
The effect rebuilds only when the Image mesh rebuilds; there is no Update loop.
Runtime code lives in `Blumgames.SlicedFill`; editor code is excluded from builds.

The checkbox automatically manages a small hidden `Blumgames.UI.SlicedFill`
component. It remains serialized on the object when disabled, so toggling it is
reversible. It is placed before Outline / Shadow and other mesh effects.
If you manually reorder components, keep Sliced Fill first among mesh effects.

## Requirements and compatibility

- Unity **2022.3 LTS or newer**, with **Unity UI (uGUI)** installed.
- Sprite Editor is only needed when authoring sprite borders.
- Does not require TextMeshPro, DOTween or any third-party plugin.
- Works with the standard uGUI Image. It is not a UI Toolkit feature.
- Preserve Aspect is ignored while enabled, matching standard Sliced behavior.
- Like standard Filled, the clickable area is not reduced to the visible fill.
- This package extends the standard Image inspector. Another plugin replacing
  that same inspector may take priority. It does not override custom Image subclasses.

## Removing the package

Remove the hidden SlicedFill components from objects and prefabs before deleting
the package folder. Use the Inspector's Debug mode to expose hidden components.
Deleting only the scripts would leave missing-component references.

All package files are under `Assets/Blumgames/SlicedFill`.
