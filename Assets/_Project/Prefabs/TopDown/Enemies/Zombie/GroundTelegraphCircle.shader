Shader "Custom/GroundTelegraphCircle"
{
    Properties
    {
        _Color ("Edge Color", Color) = (0.8, 0.05, 0.02, 0.6)
        _FillColor ("Fill Color", Color) = (0.6, 0.02, 0.0, 0.25)
        _FillProgress ("Fill Progress", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width", Range(0.01, 0.1)) = 0.04
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 0.3)) = 0.1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+1" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 100

        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            fixed4 _FillColor;
            float _FillProgress;
            float _EdgeWidth;
            float _PulseSpeed;
            float _PulseAmount;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv - 0.5;
                float dist = length(centered) * 2.0; // 0 at center, 1 at edge

                // Discard outside circle
                if (dist > 1.0)
                    discard;

                float progress = saturate(_FillProgress);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount * progress;

                // Current fill radius (0..1)
                float fillRadius = progress * pulse;

                // Fill area
                float fillMask = smoothstep(fillRadius, fillRadius - 0.05, dist) * 0.6;

                // Edge ring at current fill radius
                float edgeDist = abs(dist - fillRadius);
                float edgeMask = smoothstep(_EdgeWidth, 0.0, edgeDist) * progress;

                // Outer boundary ring (always visible once started)
                float outerDist = abs(dist - 1.0);
                float outerMask = smoothstep(_EdgeWidth * 0.7, 0.0, outerDist) * 0.4 * progress;

                // Combine
                fixed4 col = _FillColor * fillMask + _Color * (edgeMask + outerMask);
                col.a = saturate(fillMask * _FillColor.a + edgeMask * _Color.a + outerMask * _Color.a * 0.5);

                // Fade overall with progress
                col.a *= smoothstep(0.0, 0.15, progress);

                return col;
            }
            ENDCG
        }
    }
}
