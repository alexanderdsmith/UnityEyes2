Shader "EyeShader" {

	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_BumpTex ("Bump", 2D) = "bump" {}
		_GlossTex ("Glossiness", 2D) = "white" {}
		_RefractiveIdx ("Refractive index", Range(1,2)) = 1.3
		_PupilSize ("Pupil size change", Range(-1,1)) = 0
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
		
		void vert (inout appdata_full v, out Input o) {
        	UNITY_INITIALIZE_OUTPUT(Input, o);
        	o.worldSpaceViewDir = WorldSpaceViewDir(v.vertex);
		}
		
		void surf (Input IN, inout SurfaceOutputStandard o) {
			
			o.Normal = UnpackNormal (tex2D (_BumpTex, IN.uv_MainTex));
			
			float3 worldNormal = normalize(WorldNormalVector(IN, float3(0.0,0.0,0.1)));
			float3 viewDir = _WorldSpaceCameraPos - IN.worldPos;
		
			float3 frontNormalW = normalize(
				mul((float3x3) _Object2World, float3(0.0,0.0,1.0)));
			
			float heightW = saturate(dot(
				IN.worldPos - mul((float3x3) _Object2World, float3(0.0,0.0,0.0109)),
				frontNormalW));
			
			float3 refractedW = refract(
				normalize(viewDir)*-1,
				normalize(worldNormal),
				1.0/_RefractiveIdx);
		
			float cosAlpha = dot(frontNormalW, -refractedW);
			float dist = heightW / cosAlpha;
			float3 offsetW = dist * refractedW;
			float3 offsetL = mul((float3x3) _World2Object, offsetW);
			
			// clamp offset to 12mm in total to avoid over-refraction
			offsetL = clamp(offsetL, float3(-0.006,-0.006,-0.006), float3(0.006,0.006,0.006));
			
			float2 offsetL2 = float2(offsetL.x, offsetL.y);
			float2 uv = IN.uv_MainTex;
			uv += float2(-1.0, 1.0)*offsetL2 * float2(24,24);
			
			float2 offset_from_centre = (float2(0.5, 0.5) - uv) * heightW;
			uv += offset_from_centre * _PupilSize * 3;

			o.Albedo = tex2D (_MainTex, uv).rgb * 0.85;
			o.Smoothness = saturate(tex2D(_GlossTex, uv).rgb * 1.2);
			o.Alpha = 0.5f;
		}
		
		ENDCG
	} 
	Fallback "Diffuse"
}
