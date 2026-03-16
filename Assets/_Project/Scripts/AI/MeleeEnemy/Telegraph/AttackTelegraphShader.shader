Shader "Custom/AttackTelegraph"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.45, 0.05, 0.85)
        _CoreColor ("Core Color", Color) = (1, 0.9, 0.5, 1)
        _Intensity ("Intensity", Range(0, 8)) = 2.5
        _FillProgress ("Fill Progress", Range(0, 1)) = 0
        _BladeSharpness ("Blade Sharpness", Range(1, 30)) = 6.0
        _BladeCurve ("Blade Curve", Range(-3, 3)) = 1.2
        _InnerRadius ("Inner Circle Radius", Range(0.01, 0.15)) = 0.05
        _OuterRadius ("Outer Radius", Range(0.1, 0.5)) = 0.42
        _CenterGlow ("Center Glow Size", Range(0.01, 0.15)) = 0.07
        _GlowSoftness ("Glow Softness", Range(0.01, 0.2)) = 0.05
        _RingWidth ("Ring Width", Range(0.005, 0.03)) = 0.012
        _PulseSpeed ("Pulse Speed", Float) = 3.5
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.15
        _RotationSpeed ("Rotation Speed", Float) = 0.3
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+2" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 100

        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha One
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
            fixed4 _CoreColor;
            float _Intensity;
            float _FillProgress;
            float _BladeSharpness;
            float _BladeCurve;
            float _InnerRadius;
            float _OuterRadius;
            float _CenterGlow;
            float _GlowSoftness;
            float _RingWidth;
            float _PulseSpeed;
            float _PulseAmount;
            float _RotationSpeed;

            v2f vert(appdata v)
            {
                v2f o;

                // Billboard: always face camera
                float3 centerWorld = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 scale = float3(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21),
                    length(unity_ObjectToWorld._m02_m12_m22)
                );

                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;

                float3 worldPos = centerWorld
                    + camRight * v.vertex.x * scale.x
                    + camUp * v.vertex.y * scale.y;

                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv - 0.5;
                float dist = length(centered);

                if (dist > 0.5)
                    discard;

                // Slow rotation
                float rot = _Time.y * _RotationSpeed;
                float cosR = cos(rot);
                float sinR = sin(rot);
                float2 rotated = float2(
                    centered.x * cosR - centered.y * sinR,
                    centered.x * sinR + centered.y * cosR
                );

                float angle = atan2(rotated.y, rotated.x);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                float progress = saturate(_FillProgress);

                // ---- Curved blades (CotDG crescent style) ----
                float curvedAngle = angle + dist * _BladeCurve;

                // 4 main blades
                float blade4 = pow(abs(cos(curvedAngle * 2.0)), _BladeSharpness);

                // 4 secondary blades (smaller, between main)
                float blade4sec = pow(abs(sin(curvedAngle * 2.0)), _BladeSharpness * 1.5) * 0.3;

                float blades = max(blade4, blade4sec);

                // Progress-driven outer extent
                float currentOuter = lerp(_InnerRadius * 2.0, _OuterRadius, progress) * pulse;
                float bladeRadius = _InnerRadius + blades * (currentOuter - _InnerRadius);

                // Blade mask with soft glow
                float bladeMask = smoothstep(bladeRadius + _GlowSoftness, bladeRadius - _GlowSoftness * 0.5, dist);

                // ---- Bright center core ----
                float centerSize = _CenterGlow * progress * pulse;
                float centerMask = smoothstep(centerSize, centerSize * 0.15, dist);

                // ---- Inner ring (thin bright circle) ----
                float ringRadius = _InnerRadius * 1.2 * progress;
                float ringDist = abs(dist - ringRadius);
                float ringMask = smoothstep(_RingWidth, 0.0, ringDist) * progress * 0.7;

                // ---- Soft halo ----
                float haloSize = currentOuter * 0.8;
                float haloMask = exp(-dist * dist / max(haloSize * haloSize * 0.3, 0.001)) * progress * 0.2;

                // ---- Combine ----
                float totalMask = bladeMask + centerMask * 1.5 + ringMask + haloMask;

                // Color: center warm/bright, blades main color
                float centerFactor = saturate((centerMask * 1.5 + ringMask * 0.3) / max(totalMask, 0.001));
                fixed4 col = lerp(_Color, _CoreColor, centerFactor);

                col.rgb *= _Intensity * progress * pulse;
                col.a = saturate(totalMask) * progress * _Color.a;

                // Soft outer fade
                col.a *= smoothstep(0.5, 0.38, dist);

                return col;
            }
            ENDCG
        }
    }
}
