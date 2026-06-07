Shader "Custom/EyeCorneaWetness" {

    // Additive-only wetness/gloss layer for the cornea surface.
    //
    // Uses Blend One One (additive): the shader can only ADD light to the
    // frame, never subtract. This means black pixels are fully transparent —
    // there is no possible way for this layer to create a dark circle.
    // Only direct-light specular highlights (the wet catchlights) are visible.

    Properties {
        _Shininess   ("Shininess",  Range(0.01, 1)) = 0.9
        _Intensity   ("Intensity",  Range(0, 1))    = 0.3
        _AlphaTex    ("Alpha Mask", 2D)             = "white" {}
    }

    SubShader {
        Tags {
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "IgnoreProjector"= "True"
        }

        // Additive blend: src*1 + dst*1  →  can only brighten, never darken
        Blend One One
        ZWrite Off
        Cull Back

        CGPROGRAM
        #pragma surface surf BlinnPhong alpha:fade
        #pragma target 3.0

        sampler2D _AlphaTex;
        float _Shininess;
        float _Intensity;

        struct Input {
            float2 uv_AlphaTex;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            float mask = tex2D(_AlphaTex, IN.uv_AlphaTex).r;

            o.Albedo    = 0;                        // no diffuse — invisible in shadow
            o.Specular  = _Shininess;
            o.Gloss     = _Intensity * mask;        // specular intensity shaped by mask
            o.Alpha     = _Intensity * mask;        // overall fade
        }
        ENDCG
    }

    FallBack Off
}
