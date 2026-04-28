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
//   No rendering needed; the formula is exact for any irisSize and matches
//   real human iris diameter (~11–12 mm) at irisSize ≈ 1.
//
// PUPIL — anchored linear approximation. The shader's `_PupilSize` is a
//   signed UV-space offset multiplier (declared `Range(-1, 1)` but
//   Material.SetFloat doesn't enforce that, so values down to ~−1.5 still
//   render with mild iris artifacts). Anchored to:
//     - Alexander Smith's empirical note: `_PupilSize = -1.5` → ~2 mm.
//     - Anatomical maximum dilation: ~8 mm at the upper end of the
//       useful range, conventionally placed at `_PupilSize = 1.0`.
//   Linear fit through those two anchors:
//       pupil_diameter_mm = 2.4 * _PupilSize + 5.6
//   At `_PupilSize = 0` this yields 5.6 mm, in the middle of the typical
//   mesopic adult range (4–6 mm). The fit is a placeholder calibrated by
//   anatomical priors rather than measured pixels; future work can swap
//   in an empirical curve from a render-and-measure sweep without
//   changing the public mm contract here.

public static class EyeSizeCalibration
{
    // --- Iris: exact ---
    public const float IRIS_DIAMETER_MM_PER_UNIT_IRISSIZE = 11.7392f;

    // --- Pupil: anchored linear ---
    public const float PUPIL_MM_SLOPE     = 2.4f;   // mm per unit _PupilSize
    public const float PUPIL_MM_INTERCEPT = 5.6f;   // mm at _PupilSize = 0

    // Useful clamp range. Below ~1.6 mm the iris rendering shows
    // artifacts (per the maintainer); above ~8.4 mm we're outside
    // physiological range.
    public const float PUPIL_MM_MIN = 1.6f;
    public const float PUPIL_MM_MAX = 8.4f;

    // Iris-size mm → multiplier conversion. Bypassing the divide-by-zero
    // case is unnecessary (the constant is non-zero by construction).
    public static float IrisDiameterMmToIrisSize(float mm)
    {
        return mm / IRIS_DIAMETER_MM_PER_UNIT_IRISSIZE;
    }

    public static float IrisSizeToIrisDiameterMm(float irisSize)
    {
        return IRIS_DIAMETER_MM_PER_UNIT_IRISSIZE * irisSize;
    }

    public static Vector2 IrisDiameterMmRangeToIrisSizeRange(Vector2 mmRange)
    {
        return new Vector2(
            IrisDiameterMmToIrisSize(mmRange.x),
            IrisDiameterMmToIrisSize(mmRange.y));
    }

    // Pupil-size mm → multiplier conversion.
    public static float PupilDiameterMmToPupilSize(float mm)
    {
        return (mm - PUPIL_MM_INTERCEPT) / PUPIL_MM_SLOPE;
    }

    public static float PupilSizeToPupilDiameterMm(float pupilSize)
    {
        return PUPIL_MM_SLOPE * pupilSize + PUPIL_MM_INTERCEPT;
    }

    public static Vector2 PupilDiameterMmRangeToPupilSizeRange(Vector2 mmRange)
    {
        return new Vector2(
            PupilDiameterMmToPupilSize(mmRange.x),
            PupilDiameterMmToPupilSize(mmRange.y));
    }
}
