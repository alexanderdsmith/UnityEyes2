 Shader "Custom/WetnessShader" {

	Properties {
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0.0)
		_Shininess ("Shininess", Range (0.01, 1)) = 0.03
		_AlphaTex ("Texture", 2D) = "white" {}
	}
	
	SubShader {
		Tags {
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
		}
		LOD 300
		
		CGPROGRAM
		
		#include "UnityPBSLighting.cginc"
		#pragma surface surf StandardSpecular alpha:fade
		
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		
//		sampler2D _MainTex;
		sampler2D _AlphaTex;
		
		struct Input {
			float2 uv_AlphaTex;
		};
		
		void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
			o.Albedo = 0;
//			o.Albedo.a = 0.0;
			o.Smoothness = 0.9;
			o.Specular = 0.5;
			o.Alpha = lerp(0.0,0.1, tex2D (_AlphaTex, IN.uv_AlphaTex));
		}
		ENDCG
	}
	FallBack "Transparent/VertexLit"
}
