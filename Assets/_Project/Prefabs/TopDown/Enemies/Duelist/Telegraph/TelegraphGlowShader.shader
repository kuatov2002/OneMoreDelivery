Shader "Custom/TelegraphGlow"
{
    Properties
    {
        _Color ("Core Color", Color) = (1, 0.3, 0.05, 1)
        _RimColor ("Rim Color", Color) = (1, 0.1, 0.0, 0.8)
        _Intensity ("Intensity", Float) = 1.0
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _PulseSpeed ("Pulse Speed", Float) = 4.0
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.2
        _NoiseScale ("Noise Scale", Float) = 3.0
        _NoiseSpeed ("Noise Speed", Float) = 2.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+2" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha One
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldViewDir : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            fixed4 _Color;
            fixed4 _RimColor;
            float _Intensity;
            float _RimPower;
            float _PulseSpeed;
            float _PulseAmount;
            float _NoiseScale;
            float _NoiseSpeed;

            // Simple pseudo-noise
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise3d(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                         lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                         lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y),
                    f.z);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldViewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.worldViewDir);

                // Fresnel / rim
                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = pow(rim, _RimPower);

                // Pulse
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                // Animated noise for organic feel
                float n = noise3d(i.worldPos * _NoiseScale + _Time.y * _NoiseSpeed);
                float noiseMask = 0.7 + 0.3 * n;

                // Core glow (center bright, edges rim-colored)
                fixed4 coreColor = _Color * _Intensity * pulse * noiseMask;
                fixed4 rimColor = _RimColor * rim * _Intensity * pulse;

                fixed4 result = coreColor + rimColor;
                result.a = saturate(rim * 0.8 + 0.4) * _Intensity * pulse;

                return result;
            }
            ENDCG
        }
    }
}
