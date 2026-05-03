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
            
            // Procedural pupil mask. The previous design distorted UVs by
            // the formula
            //     uv += (0.5 - uv) * heightW * _PupilSize * 3;
            // and then sampled the texture at the shifted UV. At
            // |_PupilSize| beyond ~1.5 the shifted UV landed outside the
            // iris region of the texture and bleached the iris to sclera
            // white. Instead we keep `uv` un-distorted (so iris colors
            // are read straight from the texture) and apply the same
            // pupil-boundary inequality EyeSizeCalibration.cs uses to
            // decide pupil-vs-iris per fragment:
            //     |uv - 0.5| * |1 - heightW * _PupilSize * 3|  <=  r_pupil_uv
            // is true inside the apparent pupil. We render those
            // fragments as a near-black pupil colour and leave the iris
            // fragments to sample the texture at their natural UV.
            //
            // This decouples pupil rendering from texture content and
            // supports arbitrarily small pupils without iris distortion.
            const float R_PUPIL_UV = 0.0788;        // texture pupil/iris boundary
            float2 d_centre = uv - float2(0.5, 0.5);
            float uv_radius = length(d_centre);
            float pupil_factor = abs(1.0 - heightW * _PupilSize * 3.0);
            float pupil_score = uv_radius * pupil_factor;
            float pupil_mask = 1.0 - smoothstep(R_PUPIL_UV - 0.004,
                                                 R_PUPIL_UV + 0.004,
                                                 pupil_score);

            // For iris fragments whose *post-refraction* UV lands inside
            // the texture's natural pupil disk (the `uv_radius < R_PUPIL_UV`
            // test below uses the post-refraction `uv`, since that is the
            // texel actually sampled), we can't sample the texture there —
            // that would reveal the texture's built-in pupil under the
            // procedural one. Re-route those samples to the iris texture's
            // *inner edge*, just outside the pupil boundary
            // (R_PUPIL_UV + REROUTE_EPSILON).
            //
            // Two design choices that together remove the visible ring:
            //
            //   1. Use the *un-refracted* mesh UV (IN.uv_MainTex) for the
            //      angular direction. Cornea-apex fragments all have
            //      un-refracted uv0 close to (0.5, 0.5), but the
            //      refraction offset adds a directional bias common to
            //      neighbouring fragments. Using the post-refraction uv
            //      direction makes them cluster on the same iris texel,
            //      showing as a streak above the pupil at extreme
            //      negative _PupilSize. Using d0 (un-refracted) lets
            //      each apex fragment sample its own angular position.
            //
            //   2. Reroute to the iris *inner edge* (radius
            //      R_PUPIL_UV + REROUTE_EPSILON), not the iris-band
            //      midpoint 0.105. The texture's color just outside the
            //      pupil boundary continuously matches what neighbouring
            //      natural samples (uv_radius slightly > R_PUPIL_UV)
            //      produce. Sampling at 0.105 (mid-band) instead gave a
            //      brighter, color-saturated iris band that didn't
            //      match the natural-sample colors at the boundary.
            //
            // Outer fragments still sample at the post-refraction `uv`
            // (preserves the cornea's lens-like depth shift on the
            // visible iris position).
            const float REROUTE_EPSILON = 0.005;
            float2 d0 = IN.uv_MainTex - float2(0.5, 0.5);
            float r0 = length(d0);
            float2 uv_iris = uv;
            if (uv_radius < R_PUPIL_UV)
            {
                float2 dir = (r0 > 1e-5)
                    ? (d0 / r0)
                    : float2(1.0, 0.0);
                uv_iris = float2(0.5, 0.5) + dir * (R_PUPIL_UV + REROUTE_EPSILON);
            }

            float4 irisColor = tex2D(_MainTex, uv_iris);
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