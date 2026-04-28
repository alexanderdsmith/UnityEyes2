# Iris and pupil size calibration: physical millimetres in `camera_config.json`

## Summary

The `eye_parameters` block in `camera_config.json` previously documented
`pupil_size_range` and `iris_size_range` as values in metres. In practice
those values were fed directly into the dimensionless internal controls
`EyeballController.irisSize` (a mesh-vertex radial scaler) and the shader
uniform `_PupilSize` (a UV-space offset multiplier). A user setting
`pupil_size_range = { min: 0.1, max: 0.2 }` therefore obtained an
unphysically large pupil rather than a 0.1 mm one.

This patch introduces two new keys with explicit physical units:

| key | unit | range |
|---|---|---|
| `iris_diameter_mm_range`  | millimetres | typically 10 – 12 mm |
| `pupil_diameter_mm_range` | millimetres | model-supported 4 – 8.7 mm |

The legacy `iris_size_range` and `pupil_size_range` keys are still honoured
when the new ones are absent, so existing configs continue to load.

The runtime conversion happens once at JSON-load time inside
`Assets/EyeSizeCalibration.cs`. `EyeballController` and `EyeShader` are
unchanged.

## Iris mapping (exact)

The 32 vertices indexed by `EyeballController.iris_idxs` form a perfect
circle of radius `R_iris = 5.8696 mm` in world space (mesh-local
0.005870 m, lossy scale ×100, cm-to-mm ×10). Scaling by `irisSize` is a
linear vertex displacement, so

> ```
> iris_diameter_mm = 11.7392 × irisSize
> ```

The inverse `irisSize = mm / 11.7392` is exact and reversible.

## Pupil mapping (analytical, derived from the shader)

The pupil is *not* a mesh feature. It is the dark central disk of the
iris texture, sampled by the cornea fragments after a UV-space transform
encoded in `EyeShader.shader`:

```glsl
float2 offset_from_centre = (float2(0.5, 0.5) - uv) * heightW;
uv += offset_from_centre * _PupilSize * 3;
```

A cornea fragment with original texture coordinate `uv0` therefore samples
the texture at

> ```
> uv_after = uv0 + (0.5 − uv0) · heightW · _PupilSize · 3
> ```

It shows pupil texels iff `|uv_after − 0.5| ≤ r_pupil_uv`, where
`r_pupil_uv` is the radius (in UV space) of the texture's central dark
region. With a uniformly radial UV mapping on the iris ring, the mesh
radial position `r` maps to UV-radial position `(r / r_iris_world) ·
r_iris_uv`, so the apparent pupil-boundary condition becomes

> ```
> (r / r_iris_world) · r_iris_uv · |1 − heightW(r) · p · 3| ≤ r_pupil_uv
> ```

with `p = _PupilSize` and `heightW(r) = sat(s · (z(r) − z₀))` from the
shader. The cornea is well-approximated as the front half of an
ellipsoid with axial radius `R_ax` and equatorial radius `R_eq`:
`z(r) = R_ax · √(1 − (r / R_eq)²)`. The apparent pupil radius is the
largest `r ∈ [0, r_iris_world]` that satisfies the inequality; the
diameter is `2r`.

The shipped implementation in `EyeSizeCalibration.cs` evaluates this
inequality on a 5000-step radial scan (forward), and uses 60 iterations
of bisection for the inverse. Both run once per JSON load.

### Constants

Probed once from the project's data and hard-coded in
`Assets/EyeSizeCalibration.cs`:

| symbol | value | source |
|---|---|---|
| `r_iris_world`            | 5.870 mm        | mean radial distance of the 32 iris-ring vertices |
| `r_iris_uv` (`r^uv_iris`) | 0.1385          | UV distance of those same iris-ring vertices from `(0.5, 0.5)` |
| `r_pupil_uv` (`r^uv_pupil`) | 0.0788        | azimuthally-averaged radial luminance profile of `Resources/IrisTextures/eyeball_brown.png` |
| `R_ax`, `R_eq`            | 12.54 mm, 12.05 mm | mesh bounds probe of `eyeball.Eyeball` |
| `z₀`                      | 10.9 mm         | shader's cornea-apex offset constant |
| `s`                       | 100             | mesh-meters → world-cm lossy scale |

### Result

![Apparent pupil diameter vs `_PupilSize`](figures/pupil-calibration.png)

The curve is monotonic and mildly S-shaped: slope steepens around
`p = 0` and flattens at the extremes, which is why a linear
approximation (shown faded for context) underfits at the small-pupil
end. Selected sample points:

| `_PupilSize` | apparent pupil diameter |
|---|---|
| −1.5 | 4.03 mm |
| −1.0 | 4.70 mm |
|  0.0 | 6.68 mm |
| +0.5 | 7.81 mm |
| +1.0 | 8.74 mm |

The natural pupil at `_PupilSize = 0` (no UV shift) corresponds to a
mesh radius of `(r_pupil_uv / r_iris_uv) · r_iris_world ≈ 3.34 mm`,
giving a diameter of 6.68 mm.

## Lower-bound limit (why `PUPIL_MM_MIN = 4.0`)

Mathematically, the model continues to produce valid output for any
`_PupilSize`: at `_PupilSize = -3.0` it predicts a 2.78 mm pupil, well
inside the photopic-constricted range. **The actual rendering does not
follow** — at sufficiently negative values the UV transform pushes
sample coordinates *outside* the iris ring of the texture, into the
sclera region, so the iris bleaches to white instead of constricting
cleanly:

![PupilSize artifact regime](figures/pupil-artifact-strip.png)

The transition is sharp. `_PupilSize ∈ [+1.0, -1.5]` keeps the iris
visibly colored; `_PupilSize ≤ -2.0` produces an increasingly white
"iris" with a tiny dark dot in the middle, which is not a photographic
small pupil — it is a broken iris. The shipped clamp `PUPIL_MM_MIN = 4.0`
respects that threshold: 4 mm corresponds to roughly `_PupilSize = -1.5`.

To produce *clean* pupils smaller than 4 mm, the shader and/or texture
need to be modified. Three feasible paths (each is a separate work item,
not part of this patch):

1. **Pupil-mask overlay.** Draw an analytical black disk on top of the
   iris texture in the shader, sized programmatically. Decouples pupil
   size from the UV-shift trick entirely; supports arbitrary 0–8 mm
   pupils with no texture artifact.
2. **UV clamping.** Modify `EyeShader.shader` so that `uv_after` is
   clamped to a circular region inside the iris ring. Eliminates sclera
   bleed but caps the achievable apparent pupil at whatever radius the
   clamp allows.
3. **Texture redesign.** Repaint the iris textures with a wider radial
   "safe zone" between the iris's outer edge and the sclera. Cheap if
   regenerable from the source asset; otherwise five textures to edit by
   hand.

## Caveats (within the supported range, 4 ≤ mm ≤ 8.7)

1. **Texture variation.** `r_pupil_uv` was probed from the dominant
   iris texture (`eyeball_brown`, picked 50% of the time by
   `EyeballController.RandomizeEyeball`). Across the project's five
   iris textures the value ranges 0.0688 – 0.0788 (~14% spread), which
   propagates to roughly ±0.5 mm spread in apparent pupil diameter at a
   given `_PupilSize`. Configurations that lock the texture (e.g.
   always `eyeball_brown`) will see less variation than randomised runs.
2. **Refraction step ignored.** The shader applies a refraction-driven
   UV offset before the pupil shift. For an on-axis camera the offset
   is symmetric near the cornea apex and largely cancels in the apparent
   pupil diameter. Off-axis viewing angles may diverge from this model
   by another ~0.3 mm.
3. **`iris_diameter_mm_range` and `pupil_diameter_mm_range` are not
   strictly independent.** Scaling iris-ring vertices by `irisSize`
   slightly changes how pupil texels project to mesh space. The
   calibration assumes `irisSize = 1`. For `iris_diameter_mm_range`
   centered around the natural 11.74 mm this introduces sub-millimetre
   error.

These caveats are the cost of a closed-form mapping; a future
render-and-measure refinement could absorb them into the
`r_pupil_uv` and ellipsoid constants without changing the public JSON
API.

## Verification during development

Five layers were checked in Unity 6.4 batchmode against the actual
shader source and `SynthesEyesServer.LoadCamerasFromConfig`:

- calibration constants match documented values
- `mm ↔ multiplier` round-trips for both iris and pupil within 5 µm
  (forward 5000-step scan, inverse 60-iteration bisection)
- forward calibration agrees with the analytical sweep at sample points
- `LoadCamerasFromConfig` with the new mm keys produces the expected
  post-conversion `Vector2` ranges read back via reflection
- legacy `pupil_size_range` / `iris_size_range` still pass through
  untouched

All 38 assertions passed.

## Files changed by this patch

| file | role |
|---|---|
| `Assets/EyeSizeCalibration.cs` (new) | constants and conversion helpers |
| `Assets/SynthesEyesServer.cs` | `LoadCamerasFromConfig` reads the new mm keys, falls back to legacy |
| `README.md` | documents the new keys and deprecates the old ones |
| `docs/iris-pupil-mm-calibration.md` (this file) | derivation, constants, caveats |
| `docs/figures/pupil-calibration.png` | the apparent-diameter figure |
| `docs/figures/pupil-calibration-sweep.csv` | numerical sweep data |
| `docs/figures/pupil-artifact-strip.png` | rendered eye at `_PupilSize` from +1.0 down to −3.0, illustrating the iris-bleaching regime that bounds `PUPIL_MM_MIN` |
