Shader "FaceShader" {

	Properties {
	
		_Tex3dColor ("Color Textures", 3D) = "" {}
		_Tex3dColorLd ("Color Textures (Look Down)", 3D) = "" {}
//		_Tex3dBump ("Bump Textures", 3D) = "" {}
		_Zamount ("LookDownNess", Range(0,1)) = 0.0
		
		_BumpTex ("Bump", 2D) = "" {}
		
		// For blending with sclera
		_CaruncleBlend ("Caruncle (RGB)", 2D) = "white" {}
		
		// What parts are inside the anterior margin
		_MarginMask ("MarginMask (RGB)", 2D) = "white" {}
		
		// BRDF Lookup texture, light direction on x and curvature on y.
		_BRDFTex ("BRDF Lookup (RGB)", 2D) = "gray" {}
		
		// Curvature scale. Multiplier for the curvature - best to keep this very low - between 0.02 and 0.002.
		_CurvatureScale ("Curvature Scale", Float) = 0.005
		
		// Controller for fresnel specular mask. For skin, 0.028 if in linear mode, 0.2 for gamma mode.
		_Fresnel ("Fresnel Value", Float) = 0.2
		
		// Which mip-map to use when calculating curvature. Best to keep this between 1 and 2.
		_BumpBias ("Normal Map Blur Bias", Float) = 1.5
		
		// how much you're looking down
		_LookDownNess ("LookDownNess", Range(0,1)) = 0.0
	}
	
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Skin fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler3D _Tex3dColor, _Tex3dColorLd; //, _Tex3dBump;
		sampler2D _BumpTex, _CaruncleBlend;
		float _Zamount;

		sampler2D _MarginMask;
		
		sampler2D _BRDFTex;
		sampler2D _SpecTex;
		
		float _Smoothness;
		float _LookDownNess;
		float _Person; 

		float _BumpBias;
		float _CurvatureScale;

		struct Input {
			float2 uv_MarginMask;
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};
		

		struct SurfaceOutputSkin {
			fixed3 Albedo;
			fixed3 Normal;
			fixed3 Emission;
			half Smoothness;
			half Occlusion;
			fixed Alpha;
			half3 Specular;

			fixed SSS;
			fixed3 NormalBlur;
			float Curvature;
		};

		void surf (Input IN, inout SurfaceOutputSkin o) {
		
			float2 uv = IN.uv_MarginMask;
		
			fixed4 c1 = tex3D (_Tex3dColor, float3(uv, _Zamount));
			fixed4 c2 = tex3D (_Tex3dColorLd, float3(uv, _Zamount));
			fixed4 c = lerp(c1, c2, _LookDownNess);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			
			float3 caruncleColor = float3(172.0/255.0, 89.0/255.0, 89.0/255.0);
			float3 caruncleC = lerp(tex3D (_Tex3dColor, float3(0, 0, _Zamount)), caruncleColor, 0.2);
			o.Albedo = lerp(o.Albedo, caruncleC, tex2D(_CaruncleBlend, uv).r);
//			o.Albedo.rgb = caruncleColor;
			
//			float3 n_tex = tex3D (_Tex3dBump, float3(uv, _Zamount));;
//			n += UnpackNormal(tex2D(_BumpTex_f02, uv)) * _Coeff_f02;
//			n += UnpackNormal(tex2D(_BumpTex_f03, uv)) * _Coeff_f03;
//			n += UnpackNormal(tex2D(_BumpTex_m01, uv)) * _Coeff_m01;
//			n += UnpackNormal(tex2D(_BumpTex_m02, uv)) * _Coeff_m02;
//			n += UnpackNormal(tex2D(_BumpTex_m04, uv)) * _Coeff_m04;
//			n += UnpackNormal(tex2D(_BumpTex_m05, uv)) * _Coeff_m05;
//			o.Normal = UnpackNormal(tex3D (_Tex3dBump, float3(uv, _Zamount)));
			o.Normal = UnpackNormal(tex2D(_BumpTex, uv));

			//	sample combined spec / roughness / sss / ao map
//			fixed4 combinedMap = tex2D(_SpecTex, IN.uv_MainTex);

			float is_margin = tex2D (_MarginMask, uv);
			o.Specular = lerp(0.2f, 0.6f, is_margin);
			o.Smoothness = lerp(0.4f, 0.5f, is_margin);
			o.SSS = 1.0f;

			//	//////////////////////////////////////////////////////////
			// 	Skin shader specific functions

			//	Calculate the curvature of the model dynamically
			fixed3 blurredWorldNormal = UnpackNormal( tex2Dlod ( _BumpTex, float4 ( uv, 0.0, _BumpBias ) ) );
//			fixed3 blurredWorldNormal = UnpackNormal( tex3Dlod ( _Tex3dBump, float4 ( uv, _Zamount, _BumpBias ) ) );
			//	Transform it into a world normal so we can get good derivatives from it.
			blurredWorldNormal = WorldNormalVector( IN, blurredWorldNormal );
			o.NormalBlur = blurredWorldNormal;

			//	Get the scale of the derivatives of the blurred world normal and the world position.
			#if (SHADER_TARGET > 40) //SHADER_API_D3D11
            // In DX11, ddx_fine should give nicer results.
            	float deltaWorldNormal = length( abs(ddx_fine(blurredWorldNormal)) + abs(ddy_fine(blurredWorldNormal)) );
            	float deltaWorldPosition = length( abs(ddx_fine(IN.worldPos)) + abs(ddy_fine(IN.worldPos)) );
			#else
				float deltaWorldNormal = length( fwidth( blurredWorldNormal ) );
				float deltaWorldPosition = length( fwidth ( IN.worldPos ) );
			#endif		
			o.Curvature = (deltaWorldNormal / deltaWorldPosition) * _CurvatureScale; // * combinedMap.b;
//			o.Albedo = _Color*0.5;
			
		}
		
		inline fixed4 CookTorrenceLight (SurfaceOutputSkin s, half3 viewDir, UnityGI gi) {

			s.Normal = normalize(s.Normal);

			half oneMinusReflectivity;
			s.Albedo = EnergyConservationBetweenDiffuseAndSpecular (s.Albedo, s.Specular, /*out*/ oneMinusReflectivity);

			#define oneMinusRoughness s.Smoothness
			
			half diff = dot(s.NormalBlur, gi.light.dir);
			half dotNL = max(0, diff);

			#define Pi 3.14159265358979323846
			#define OneOnLN2_x6 8.656170

			half dotNV = max(0, dot(s.Normal, viewDir) ); 			// UNITY BRDF does not normalize(viewDir) ) );
			half3 halfDir = normalize (gi.light.dir + viewDir);
			half dotNH = max (0, dot (s.Normal, halfDir));

			half dotLH = max(0, dot(gi.light.dir, halfDir));

			//	We must NOT max dotNLBlur due to Half-Lambert lighting
			float dotNLBlur = dot( s.NormalBlur, gi.light.dir);


			//	////////////////////////////////////////////////////////////
			//	Cook Torrrance
			//	from The Order 1886 // http://blog.selfshadow.com/publications/s2013-shading-course/rad/s2013_pbs_rad_notes.pdf
			half alpha = 1 - s.Smoothness; // alpha is roughness
			alpha *= alpha;
			half alpha2 = alpha * alpha;

			//	Specular Normal Distribution Function: GGX Trowbridge Reitz
			half denominator = (dotNH * dotNH) * (alpha2 - 1.f) + 1.f;
			half D = alpha2 / (Pi * denominator * denominator);
			//	Geometric Shadowing: Smith
			// B. Karis, http://graphicrants.blogspot.se/2013/08/specular-brdf-reference.html
			half G_L = dotNL + sqrt( (dotNL - dotNL * alpha) * dotNL + alpha );
			half G_V = dotNV + sqrt( (dotNV - dotNV * alpha) * dotNV + alpha );
			half G = 1.0 / (G_V * G_L);
			//	Fresnel: Schlick / fast fresnel approximation
			half F = 1 - oneMinusReflectivity + ( oneMinusReflectivity) * exp2(-OneOnLN2_x6 * dotNH );
			
			//	Lazarov 2013, "Getting More Physical in Call of Duty: Black Ops II", changed by EPIC
			const half4 c0 = { -1, -0.0275, -0.572, 0.022 };
			const half4 c1 = { 1, 0.0425, 1.04, -0.04 };
			half4 r = (1-oneMinusRoughness) * c0 + c1;
			half a004 = min( r.x * r.x, exp2( -9.28 * dotNV ) ) * r.x + r.y;
			half2 AB = half2( -1.04, 1.04 ) * a004 + r.zw;
			half3 F_L = s.Specular * AB.x + AB.y;

			//	Skin Lighting
			float2 brdfUV;
			// Half-Lambert lighting value based on blurred normals.
			brdfUV.x = dotNLBlur * 0.5 + 0.5;
			brdfUV.y = s.Curvature * dot(gi.light.color, fixed3(0.22, 0.707, 0.071));
			half3 brdf = tex2D( _BRDFTex, brdfUV ).rgb;

			//	Final composition
			half4 c = 0;
			c.rgb = s.Albedo * gi.light.color * lerp(dotNL.xxx, brdf, s.SSS) // diffuse
					+ D * G * F * gi.light.color * dotNL; 					 // direct specular
					+ gi.indirect.specular * F_L;						     // indirect specular

//			c.rgb = s.Albedo * gi.light.color * lerp(dotNL.xxx, brdf, s.SSS);
//			c.rgb = brdf * brdf;
//			c.rgb = s.Curvature;
			
			return c;
		}

		inline fixed4 LightingSkin (SurfaceOutputSkin s, half3 viewDir, UnityGI gi)
		{
			fixed4 c;
			c = CookTorrenceLight (s, viewDir, gi);

			#if defined(DIRLIGHTMAP_SEPARATE)
				#ifdef LIGHTMAP_ON
					c += UnityBlinnPhongLight (s, viewDir, gi.light2);
				#endif
				#ifdef DYNAMICLIGHTMAP_ON
					c += UnityBlinnPhongLight (s, viewDir, gi.light3);
				#endif
			#endif

			#ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
				c.rgb += s.Albedo * gi.indirect.diffuse;
			#endif

			return c;
		}

		inline void LightingSkin_GI (
			SurfaceOutputSkin s,
			UnityGIInput data,
			inout UnityGI gi)
		{
			gi = UnityGlobalIllumination (data, s.Occlusion, s.Smoothness, s.Normal, true); // reflections = true
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
