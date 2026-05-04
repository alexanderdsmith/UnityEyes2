// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "EyeShader" {

    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _BumpTex ("Bump", 2D) = "bump" {}
        _GlossTex ("Glossiness", 2D) = "white" {}
        _RefractiveIdx ("Refractive index", Range(1,2)) = 1.3
        _PupilSize ("Pupil size change", Range(-1,1)) = 0
        _Metallic ("Metallic", Range(0,1)) = 0.01       // Slightly reduced to avoid washing out
        _Glossiness ("Smoothness", Range(0,1)) = 0.95   // Increased for more reflectivity
        _ReflectionIntensity ("Reflection Intensity", Range(0,1)) = 0.9  // Increased
        _ReflectionFresnel ("Reflection Fresnel", Range(0,10)) = 3.0     // Increased
    }
    
    SubShader {
        Tags { "RenderType" = "Opaque" }
        CGPROGRAM
        
        // Physically based Standard lighting model, and enable shadows on all light types
        #include "UnityPBSLighting.cginc"
        #pragma surface surf Standard fullforwardshadows keepalpha 
//		vertex:vert
//		#include "Tessellation.cginc"
        
        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
        
        struct Input {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 viewDir;
            float3 worldSpaceViewDir;
            float3 worldNormal;
            float3 worldPos;
            INTERNAL_DATA
        };
        
        sampler2D _MainTex;
        sampler2D _BumpTex;
        sampler2D _GlossTex;
        float _RefractiveIdx, _PupilSize;
        float _Metallic;
        float _Glossiness;
        float _ReflectionIntensity;
        float _ReflectionFresnel;
        
        void vert (inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.worldSpaceViewDir = WorldSpaceViewDir(v.vertex);
        }

        // Modify the surf function:

        void surf (Input IN, inout SurfaceOutputStandard o) {
            
            o.Normal = UnpackNormal (tex2D (_BumpTex, IN.uv_MainTex));
            
            float3 worldNormal = normalize(WorldNormalVector(IN, float3(0.0,0.0,0.1)));
            float3 viewDir = _WorldSpaceCameraPos - IN.worldPos;

            float3 frontNormalW = normalize(
                mul((float3x3) unity_ObjectToWorld, float3(0.0,0.0,1.0)));
            
            float heightW = saturate(dot(
                IN.worldPos - mul((float3x3) unity_ObjectToWorld, float3(0.0,0.0,0.0109)),
                frontNormalW));
            
            float3 refractedW = refract(
                normalize(viewDir)*-1,
                normalize(worldNormal),
                1.0/_RefractiveIdx);

            float cosAlpha = dot(frontNormalW, -refractedW);
            float dist = heightW / cosAlpha;
            float3 offsetW = dist * refractedW;
            float3 offsetL = mul((float3x3) unity_WorldToObject, offsetW);
            
            // clamp offset to 12mm in total to avoid over-refraction
            offsetL = clamp(offsetL, float3(-0.006,-0.006,-0.006), float3(0.006,0.006,0.006));
            
            float2 offsetL2 = float2(offsetL.x, offsetL.y);
            float2 uv = IN.uv_MainTex;
            uv += float2(-1.0, 1.0)*offsetL2 * float2(24,24);
            
            // Pupil rendering: procedural mask + radial iris-band remap.
            //
            // Texture layout (constants from mesh + texture probes):
            //   r in [0,                R_PUPIL_UV] → natural texture pupil
            //   r in [R_PUPIL_UV,       R_IRIS_UV ] → natural iris band
            //   r >  R_IRIS_UV                       → sclera / outer
            //
            // The analytical apparent-pupil-boundary inequality
            //     r0 * |1 − heightW · _PupilSize · 3|  ≤  R_PUPIL_UV
            // gives an apparent pupil radius
            //     r_app = R_PUPIL_UV / |1 − heightW · _PupilSize · 3|
            // For _PupilSize < 0 this is < R_PUPIL_UV (smaller pupil);
            // for _PupilSize > 0 it grows.
            //
            // To make the visible pupil match r_app *and* avoid bleach
            // and avoid the rim/seam from prior reroute schemes, we do a
            // piecewise *radial* remap of the texture sample point:
            //
            //   r0 ∈ [0,       r_app  ] → masked dark pupil (r_sample
            //                              irrelevant)
            //   r0 ∈ [r_app,   R_IRIS ] → linearly stretch iris band so
            //                              [r_app, R_IRIS] on screen
            //                              maps to [R_PUPIL, R_IRIS]
            //                              in texture
            //   r0 ∈ [R_IRIS,  ∞      ] → sample naturally (r_sample = r0)
            //
            // Continuity:
            // - At r0 = R_IRIS_UV the inside and outside branches both
            //   sample at r_sample = R_IRIS_UV → no value jump at the
            //   limbus.
            // - At r0 = r_app the iris-band branch samples at r_sample
            //   = R_PUPIL_UV (the texture's natural inner-iris-ring
            //   colour), which is the colour right next to the texture's
            //   own dark pupil — so the procedural-dark to iris
            //   transition reads as a natural pupil-iris boundary.
            //
            // Net effect: for negative _PupilSize the iris band is
            // radially stretched inward to fill the larger screen iris
            // annulus. No iris colour is ever sampled past R_IRIS_UV
            // (no bleach to sclera) and no two different texels meet
            // discontinuously at the apparent boundaries (no rim).
            const float R_PUPIL_UV = 0.0788;
            const float R_IRIS_UV  = 0.1385;

            float2 d0 = IN.uv_MainTex - float2(0.5, 0.5);
            float r0 = length(d0);
            float2 dir0 = (r0 > 1e-5) ? (d0 / r0) : float2(1.0, 0.0);

            float pupil_factor = abs(1.0 - heightW * _PupilSize * 3.0);
            float r_app = R_PUPIL_UV / max(pupil_factor, 0.001);

            // Piecewise radial sample radius.
            float r_sample;
            if (r0 < R_IRIS_UV) {
                float t = saturate((r0 - r_app) /
                                   max(R_IRIS_UV - r_app, 0.001));
                r_sample = R_PUPIL_UV + (R_IRIS_UV - R_PUPIL_UV) * t;
            } else {
                r_sample = r0;
            }

            // Reconstruct the iris UV (preserve angular direction; reapply
            // the lateral refraction shift the same way the original
            // post-refraction `uv` does).
            float2 uv_iris = float2(0.5, 0.5) + dir0 * r_sample;
            uv_iris += float2(-1.0, 1.0) * offsetL2 * float2(24.0, 24.0);

            float4 irisColor = tex2D(_MainTex, uv_iris);

            // Procedural pupil mask: dark inside r0 < r_app, smooth
            // edge over a fixed half-width to anti-alias.
            float pupil_mask = 1.0 - smoothstep(r_app - 0.003,
                                                 r_app + 0.003,
                                                 r0);
            const float4 pupilColor = float4(0.02, 0.02, 0.02, 1.0);
            float4 eyeColor = lerp(irisColor, pupilColor, pupil_mask);
            
            // Determine if we're on the iris/pupil vs. the sclera (white part)
            // by checking color luminance - darker areas are iris/pupil
            float luminance = dot(eyeColor.rgb, float3(0.299, 0.587, 0.114));
            float isIris = 1.0 - smoothstep(0.2, 0.7, luminance);
            
            // Vary reflectivity based on whether we're on iris or sclera
            float baseReflectionIntensity = lerp(0.4, _ReflectionIntensity, isIris);
            
            // Use a more subtle albedo reduction to preserve color
            o.Albedo = eyeColor.rgb * (1.0 - baseReflectionIntensity * 0.2);
            
            // Get glossiness from texture
            float texGloss = saturate(tex2D(_GlossTex, uv).r);
            
            // Enhanced Fresnel effect - stronger at glancing angles
            float NdotV = saturate(dot(worldNormal, normalize(viewDir)));
            float fresnel = pow(1.0 - NdotV, _ReflectionFresnel) * baseReflectionIntensity;
            
            // Different smoothness values for iris vs. sclera
            // Sclera should be slightly less reflective than the iris for realism
            float irisGloss = lerp(_Glossiness * 0.8, _Glossiness, isIris);
            
            // Apply material properties with variation between iris and sclera
            //o.Metallic = lerp(_Metallic * 0.7, _Metallic, isIris); 
            
            // Enhanced smoothness handling
            // Base smoothness from texture, enhanced by parameter
            float baseSmooth = texGloss * irisGloss;
            
            // Apply fresnel effect to smoothness
            o.Smoothness = lerp(baseSmooth, irisGloss, fresnel);
            
            // Add a "wet film" effect to the entire eye
            // This slightly enhances reflectivity everywhere, simulating tear film
            float tearFilm = 0.65;  // Strength of the wet film effect
            o.Smoothness = max(o.Smoothness, baseSmooth + tearFilm);
            
            o.Alpha = 0.5f;
        }
        
        // void surf (Input IN, inout SurfaceOutputStandard o) {
            
        //     o.Normal = UnpackNormal (tex2D (_BumpTex, IN.uv_MainTex));
            
        //     float3 worldNormal = normalize(WorldNormalVector(IN, float3(0.0,0.0,0.1)));
        //     float3 viewDir = _WorldSpaceCameraPos - IN.worldPos;
        
        //     float3 frontNormalW = normalize(
        //         mul((float3x3) unity_ObjectToWorld, float3(0.0,0.0,1.0)));
            
        //     float heightW = saturate(dot(
        //         IN.worldPos - mul((float3x3) unity_ObjectToWorld, float3(0.0,0.0,0.0109)),
        //         frontNormalW));
            
        //     float3 refractedW = refract(
        //         normalize(viewDir)*-1,
        //         normalize(worldNormal),
        //         1.0/_RefractiveIdx);
        
        //     float cosAlpha = dot(frontNormalW, -refractedW);
        //     float dist = heightW / cosAlpha;
        //     float3 offsetW = dist * refractedW;
        //     float3 offsetL = mul((float3x3) unity_WorldToObject, offsetW);
            
        //     // clamp offset to 12mm in total to avoid over-refraction
        //     offsetL = clamp(offsetL, float3(-0.006,-0.006,-0.006), float3(0.006,0.006,0.006));
            
        //     float2 offsetL2 = float2(offsetL.x, offsetL.y);
        //     float2 uv = IN.uv_MainTex;
        //     uv += float2(-1.0, 1.0)*offsetL2 * float2(24,24);
            
        //     float2 offset_from_centre = (float2(0.5, 0.5) - uv) * heightW;
        //     uv += offset_from_centre * _PupilSize * 3;

        //     // Base color with slightly reduced intensity to make room for reflections
        //     o.Albedo = tex2D(_MainTex, uv).rgb * (1.0 - _ReflectionIntensity * 0.5);
            
        //     // Get glossiness from texture and boost it with our parameter
        //     float texGloss = saturate(tex2D(_GlossTex, uv).r);
            
        //     // Calculate view-dependent reflection using Fresnel
        //     float NdotV = saturate(dot(worldNormal, normalize(viewDir)));
        //     float fresnel = pow(1.0 - NdotV, _ReflectionFresnel) * _ReflectionIntensity;
            
        //     // Apply material properties
        //     o.Metallic = _Metallic;
        //     o.Smoothness = lerp(texGloss, _Glossiness, fresnel);
            
        //     // Apply additional specular reflection
        //     o.Smoothness = max(o.Smoothness, texGloss * _Glossiness);
            
        //     o.Alpha = 0.5f;
        // }
        
        ENDCG
    } 
    Fallback "Diffuse"
}