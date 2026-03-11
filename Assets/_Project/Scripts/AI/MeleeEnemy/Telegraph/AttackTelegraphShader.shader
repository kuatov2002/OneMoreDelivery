Shader "Custom/AttackTelegraph"
{
    Properties
    {
        _Color ("Edge Color", Color) = (1, 0.2, 0.1, 0.3)
        _FillColor ("Fill Color", Color) = (1, 0.1, 0.0, 0.5)
        _FillProgress ("Fill Progress", Range(0, 1)) = 0
        _Arc ("Arc Angle (degrees)", Float) = 60
        _Direction ("Direction Angle (degrees)", Float) = 0
        _EdgeWidth ("Edge Width", Range(0.005, 0.05)) = 0.015
        _PulseSpeed ("Pulse Speed", Float) = 3.0
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.3
        _InnerRadius ("Inner Radius", Range(0, 0.2)) = 0.05
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+1" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 100

        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            fixed4 _Color;
            fixed4 _FillColor;
            float _FillProgress;
            float _Arc;
            float _Direction;
            float _EdgeWidth;
            float _PulseSpeed;
            float _PulseIntensity;
            float _InnerRadius;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Center UVs so origin is at (0,0)
                float2 centered = i.uv - 0.5;
                float dist = length(centered);

                // Discard outside circle and inside inner radius
                if (dist > 0.5 || dist < _InnerRadius)
                    discard;

                // Normalize distance to 0..1 range
                float normDist = dist / 0.5;

                // Calculate angle of this pixel relative to forward direction
                // atan2(x, y) gives angle from +Y axis (forward in UV space)
                float pixelAngle = atan2(centered.x, centered.y);
                float dirRad = _Direction * 0.0174533; // degrees to radians
                float relativeAngle = pixelAngle - dirRad;

                // Wrap to -PI..PI
                relativeAngle = relativeAngle - 6.2831853 * floor((relativeAngle + 3.1415927) / 6.2831853);

                float halfArcRad = (_Arc * 0.5) * 0.0174533;

                // Check if pixel is within the arc sector
                float absAngle = abs(relativeAngle);
                if (absAngle > halfArcRad)
                    discard;

                // Pulse effect
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseIntensity;

                // Edge detection: outer rim and arc sides
                float outerEdge = smoothstep(0.5, 0.5 - _EdgeWidth, dist);
                float innerEdge = smoothstep(_InnerRadius, _InnerRadius + _EdgeWidth, dist);
                float sideEdge = smoothstep(halfArcRad, halfArcRad - _EdgeWidth * 3, absAngle);

                float edgeMask = 1.0 - outerEdge * innerEdge * sideEdge;

                // Fill: radial from center outward
                float fillMask = step(normDist, _FillProgress);

                // Combine: edge is always visible, fill fades in
                fixed4 edgeColor = _Color;
                edgeColor.a *= pulse;

                fixed4 fillColor = _FillColor;
                fillColor.a *= fillMask * pulse * (0.6 + 0.4 * normDist);

                // Final color: edge on top of fill
                fixed4 result = lerp(fillColor, edgeColor, edgeMask * 0.8);
                result.a = max(fillColor.a, edgeColor.a * edgeMask);

                // Fade near edges of arc for softer look
                float arcFade = smoothstep(halfArcRad, halfArcRad - 0.05, absAngle);
                result.a *= arcFade;

                return result;
            }
            ENDCG
        }
    }
}
