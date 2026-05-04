using UnityEngine;

// Maps physical eye dimensions (millimeters) to the dimensionless internal
// controls EyeballController consumes (`irisSize` mesh-vertex scaler and
// the shader's `_PupilSize` UV multiplier).
//
// Calibration sources:
//
// IRIS — analytical, exact. The 32 vertices indexed by EyeballController.
//   iris_idxs form a circle of radius R = 5.8696 mm (mesh-local 0.005870 m
//   × 100 lossyScale × 10 cm-to-mm). Scaling those vertices by `irisSize`
//   scales the world iris diameter linearly:
//       iris_diameter_mm = 2 * R * irisSize = 11.7392 * irisSize
//
// PUPIL — analytical, derived directly from the shader UV math
//
//   The shader (Assets/Eyeball/EyeShader.shader) computes:
//       uv_after = uv0 + (0.5 − uv0) × heightW × _PupilSize × 3
//   so a cornea point at UV uv0 with cornea-bulge factor heightW shows
//   pupil texels iff |uv_after − 0.5| ≤ r_pupil_uv, equivalently
//       |uv0 − 0.5| × |1 − heightW × _PupilSize × 3| ≤ r_pupil_uv.
//
//   Substituting uv0_radius = (r / r_iris_world) × r_iris_uv (because
//   the iris ring vertices map mesh-radial r_iris_world to UV-radial
//   r_iris_uv), the apparent pupil radius for a given _PupilSize is
//       max{r ∈ [0, r_iris_world] :
//             (r / r_iris_world) × r_iris_uv × |1 − heightW(r) × ps × 3| ≤ r_pupil_uv}
//   where heightW(r) = saturate(LossyScale × (z(r) − cornea_offset))
//   and z(r) is the front-half ellipsoid height at radial distance r.
//
//   This is implemented directly below — no polynomial fit, no
//   interpolation. The forward direction (PupilSize → mm) is a fine
//   1-D scan (5000 steps over r). The inverse (mm → PupilSize) is a
//   bisection on a monotonic function — both run once at JSON load.
//
// Constants below come from the Phase 1 mesh probe (iris ring + ellipsoid
// fit) and the Phase 2 texture pupil-radius probe across the project's
// five iris textures (range 0.0688 – 0.0788, ~14% spread).
//
// The calibration is anchored to `eyeball_brown` (r_pupil_uv = 0.0788),
// the texture EyeballController.RandomizeEyeball picks 50% of the time.
// Switching to one of the other four textures introduces up to ~0.5 mm
// of additional variation in apparent pupil diameter at the same
// `_PupilSize` — pin the texture (e.g. via SetTexture) for fully
// deterministic millimetre output.
//
// Refraction caveat: the shader applies a refraction-driven UV offset
// before the pupil shift. For an on-axis camera looking at the eye
// the refraction term is symmetric near the apex and largely cancels in
// the apparent pupil diameter, but at off-axis viewing angles the
// rendered pupil may differ by another ~0.3 mm. The mapping is therefore
// "best-effort given mesh + shader math, on-axis view".

public static class EyeSizeCalibration
{
    // --- Iris: exact ---
    public const float IRIS_DIAMETER_MM_PER_UNIT_IRISSIZE = 11.7392f;

    // --- Pupil: analytical-shader-math constants ---
    // Mesh probe (iris ring vertices form a perfect circle).
    public const float IRIS_RING_RADIUS_M  = 0.005870f;   // mesh-local meters
    public const float IRIS_RING_UV_RADIUS = 0.1385f;     // UV-space radius

    // Texture probe — anchored to eyeball_brown (the texture
    // EyeballController.RandomizeEyeball picks 50% of the time). See
    // header comment for the texture-set spread.
    public const float PUPIL_TEXTURE_UV_RADIUS = 0.0788f;

    // Eyeball ellipsoid constants from EyeballRadiusProbe (mesh meters).
    const float ELLIPSOID_AX_M  = 0.01254f;
    const float ELLIPSOID_EQ_M  = 0.01205f;
    const float CORNEA_OFFSET_M = 0.0109f;
    const float LOSSY_SCALE     = 100f;       // mesh m → world cm (probe-confirmed)

    // Supported clamp range for mm input. The lower bound was 4.0 mm
    // before the EyeShader was patched (see Assets/Eyeball/EyeShader.shader
    // and docs/iris-pupil-mm-calibration.md); since the radial-stretch
    // shader no longer bleaches the iris at extreme negative _PupilSize,
    // the lower bound is now set by the analytical model's saturation
    // floor at the bisection's most-negative bound:
    //     forward(_PupilSize = -3.5) ≈ 2.52 mm
    // Inputs below 2.5 mm are clamped at JSON load (SynthesEyesServer);
    // inputs above 8.7 mm exceed the analytical reach and are similarly
    // clamped. The SynthesEyesServer reader logs a Debug.LogWarning when
    // clamping fires so the user is told the JSON value didn't survive.
    public const float PUPIL_MM_MIN = 2.5f;
    public const float PUPIL_MM_MAX = 8.7f;

    // ---- Iris helpers (linear, exact) ----
    public static float IrisDiameterMmToIrisSize(float mm) =>
        mm / IRIS_DIAMETER_MM_PER_UNIT_IRISSIZE;
    public static float IrisSizeToIrisDiameterMm(float irisSize) =>
        IRIS_DIAMETER_MM_PER_UNIT_IRISSIZE * irisSize;
    public static Vector2 IrisDiameterMmRangeToIrisSizeRange(Vector2 mmRange) =>
        new Vector2(IrisDiameterMmToIrisSize(mmRange.x),
                    IrisDiameterMmToIrisSize(mmRange.y));

    // ---- Pupil: forward (PupilSize → mm) directly from shader math ----
    // Returns apparent pupil DIAMETER in millimeters at the eye plane.
    public static float PupilSizeToPupilDiameterMm(float pupilSize)
    {
        const int STEPS = 5000;
        float bestR = 0f;
        for (int s = 0; s <= STEPS; s++)
        {
            float r = IRIS_RING_RADIUS_M * s / (float)STEPS;        // mesh meters
            float z = EllipsoidZ(r);
            float heightW = Mathf.Max(0f, LOSSY_SCALE * (z - CORNEA_OFFSET_M));
            float uv_radius = (r / IRIS_RING_RADIUS_M) * IRIS_RING_UV_RADIUS;
            float factor   = Mathf.Abs(1f - heightW * pupilSize * 3f);
            if (uv_radius * factor <= PUPIL_TEXTURE_UV_RADIUS)
                bestR = r;
        }
        return 2f * bestR * 1000f;            // mesh meters → mm
    }

    // ---- Pupil: inverse (mm → PupilSize) via bisection ----
    // The forward function is monotonic over the bisection bounds
    // [-3.5, +1.5] (it stays monotonic past +1.5 too, but the apparent
    // pupil reaches PUPIL_MM_MAX before then). Outside [-3.5, +1.5] the
    // factor `1 − heightW · ps · 3` eventually crosses zero and the
    // forward function turns non-monotonic, but those values aren't
    // physically meaningful and aren't reachable from the supported
    // mm input range. Bisects to within 1e-6 of `_PupilSize`.
    public static float PupilDiameterMmToPupilSize(float mm)
    {
        // Bisection bounds. forward(-3.5) ≈ 2.52 mm sets the achievable
        // mm floor; PUPIL_MM_MIN = 2.5 mm is set just at this floor.
        // forward(+1.5) ≈ 9.41 mm is comfortably above PUPIL_MM_MAX = 8.7.
        float lo = -3.5f, hi = 1.5f;
        for (int i = 0; i < 60; i++)
        {
            float mid = 0.5f * (lo + hi);
            float predicted = PupilSizeToPupilDiameterMm(mid);
            if (predicted < mm) lo = mid;
            else                hi = mid;
            if (hi - lo < 1e-6f) break;
        }
        return 0.5f * (lo + hi);
    }

    public static Vector2 PupilDiameterMmRangeToPupilSizeRange(Vector2 mmRange) =>
        new Vector2(PupilDiameterMmToPupilSize(mmRange.x),
                    PupilDiameterMmToPupilSize(mmRange.y));

    // Front-half ellipsoid z(r) at radial distance r from optical axis.
    static float EllipsoidZ(float r)
    {
        float ratio = Mathf.Clamp01(r / ELLIPSOID_EQ_M);
        return ELLIPSOID_AX_M * Mathf.Sqrt(1f - ratio * ratio);
    }
}
