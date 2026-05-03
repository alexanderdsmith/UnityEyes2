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

            // The pupil mask AND the iris sampling both work in the
            // *un-refracted* mesh-UV coordinate system. Earlier
            // implementations split the two: pupil mask was driven by
            // post-refraction `uv` while iris samples were rerouted using
            // un-refracted `d0`. Those two coordinate systems differ
            // wherever refraction is non-trivial (off-axis, near the
            // cornea apex), and the procedural-pupil/rerouted-iris
            // boundary fell on the post-refraction circle while the
            // rerouted iris colours used un-refracted angular positions
            // — leaving a visible "ring" between inner and outer iris
            // colour bands.
            //
            // Unifying on un-refracted UV makes the boundary trivially
            // continuous: a single `uv_iris = 0.5 + dir0 * max(r0, ...)`
            // formula serves both inside and outside the pupil. The
            // pupil mask boundary lies on the same un-refracted circle
            // the iris samples follow, so they line up perfectly.
            //
            // Trade-off: the iris loses the lateral refraction shift
            // (the cornea's lens-like sideways displacement of the iris
            // texture). For an on-axis camera that shift is small and
            // largely radially symmetric — barely noticeable — and well
            // worth losing to fix the boundary artifact. The cornea's
            // apparent-pupil-deformation factor `heightW * _PupilSize *
            // 3` still depends on the cornea bulge geometry (heightW),
            // so apparent pupil size still tracks the analytical model.
            const float REROUTE_EPSILON = 0.005;
            float2 d0 = IN.uv_MainTex - float2(0.5, 0.5);
            float r0 = length(d0);
            float2 dir0 = (r0 > 1e-5) ? (d0 / r0) : float2(1.0, 0.0);

            float pupil_factor = abs(1.0 - heightW * _PupilSize * 3.0);
            float pupil_score = r0 * pupil_factor;
            float pupil_mask = 1.0 - smoothstep(R_PUPIL_UV - 0.004,
                                                 R_PUPIL_UV + 0.004,
                                                 pupil_score);

            // Iris sample radius = un-refracted radius, clamped to ≥
            // R_PUPIL_UV + ε so we never sample inside the texture's
            // natural pupil disk. At the boundary r0 = R_PUPIL_UV the
            // formula reduces to the same point on either side, so the
            // transition is continuous.
            float r_iris = max(r0, R_PUPIL_UV + REROUTE_EPSILON);
            float2 uv_iris = float2(0.5, 0.5) + dir0 * r_iris;
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