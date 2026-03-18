Shader "UI/DashCharge"
{
    Properties
    {
        _FillAmount     ("Fill Amount", Range(0, 1))       = 1
        _OutlineColor   ("Outline Color", Color)           = (0.85, 0.92, 1, 1)
        _FillColor      ("Fill Color", Color)              = (0.7, 0.85, 1, 0.85)
        _EmptyFillColor ("Empty Fill Color", Color)        = (0.1, 0.1, 0.12, 0.45)
        _OutlineWidth   ("Outline Width", Range(0.01, 0.2)) = 0.07
        _Radius         ("Circle Radius", Range(0.2, 0.5))  = 0.42
        _GlowWidth      ("Glow Width", Range(0, 0.2))       = 0.1
        _GlowIntensity  ("Glow Intensity", Range(0, 5))     = 2.5
        _PulseSpeed     ("Pulse Speed", Range(0, 10))        = 3.0
        _PulseAmount    ("Pulse Amount", Range(0, 0.5))      = 0.2
        _Charged        ("Charged", Float)                   = 1
        [HideInInspector] _MainTex ("MainTex", 2D)              = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Overlay"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
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
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            float  _FillAmount;
            float4 _OutlineColor;
            float4 _FillColor;
            float4 _EmptyFillColor;
            float  _OutlineWidth;
            float  _Radius;
            float  _GlowWidth;
            float  _GlowIntensity;
            float  _PulseSpeed;
            float  _PulseAmount;
            float  _Charged;

            #define PI 3.14159265

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                o.color  = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv   = i.uv - 0.5;
                float  dist  = length(uv);

                // ---- radial fill angle (clockwise from top) ----
                float angle = atan2(uv.x, uv.y);           // [-PI, PI], 0 = top
                float norm  = angle / (2.0 * PI);           // [-0.5, 0.5]
                norm = norm < 0.0 ? norm + 1.0 : norm;     // [0, 1] clockwise from top

                float isFilled = _FillAmount >= 0.999
                               ? 1.0
                               : step(norm, _FillAmount);

                // when FillAmount is 0, nothing filled
                isFilled *= step(0.001, _FillAmount);

                // ---- geometry masks ----
                float outerR  = _Radius;
                float innerR  = _Radius - _OutlineWidth;
                float glowR   = _Radius + _GlowWidth;

                float aa = 0.015; // anti-alias width

                float outerMask = 1.0 - smoothstep(outerR - aa, outerR, dist);
                float innerMask = smoothstep(innerR - aa, innerR, dist);
                float glowMask  = 1.0 - smoothstep(outerR, glowR, dist);

                float outlineRing = outerMask * innerMask;    // the ring itself
                float innerDisc   = outerMask * (1.0 - innerMask); // inner area

                // ---- pulse when charged ----
                float pulse = 1.0;
                if (_Charged > 0.5)
                {
                    pulse = 1.0 + _PulseAmount * sin(_Time.y * _PulseSpeed);
                }

                // ---- compose colors ----
                // outline — always visible, bright when charged
                float4 olColor = _OutlineColor;
                olColor.rgb *= _Charged > 0.5 ? _GlowIntensity * pulse : 0.6;

                // inner fill — lerp between empty and filled based on radial progress
                float4 innerColor = lerp(_EmptyFillColor, _FillColor, isFilled);
                innerColor.rgb *= _Charged > 0.5 && isFilled > 0.5 ? pulse : 1.0;

                // outer glow — only when charged
                float4 glowColor = _OutlineColor;
                glowColor.a   = glowMask * 0.5 * (_Charged > 0.5 ? pulse : 0.15);
                glowColor.rgb *= _GlowIntensity * 0.5;

                // final composite
                float4 col = float4(0, 0, 0, 0);

                // layer 1: outer glow (behind everything)
                col = glowColor;

                // layer 2: inner disc
                float4 blended = innerColor;
                blended.a *= innerDisc;
                col.rgb = lerp(col.rgb, blended.rgb, blended.a);
                col.a   = max(col.a, blended.a);

                // layer 3: outline ring on top
                float4 olBlended = olColor;
                olBlended.a *= outlineRing;
                col.rgb = lerp(col.rgb, olBlended.rgb, olBlended.a);
                col.a   = max(col.a, olBlended.a);

                col *= i.color;

                return col;
            }
            ENDCG
        }
    }
}
