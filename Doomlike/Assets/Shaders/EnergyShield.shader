Shader "Doomlike/EnergyShield"
{
    Properties
    {
        _BaseColor("Base Color (rgb, a = body alpha)", Color) = (0.15, 0.55, 1.0, 0.10)
        [HDR] _RimColor("Rim Color (HDR)", Color) = (0.5, 0.9, 1.0, 1.0)
        _RimPower("Rim Power", Range(0.25, 8)) = 2.5
        _RimIntensity("Rim Intensity", Range(0, 5)) = 1.0

        _PatternScale("Pattern Scale", Range(0.1, 30)) = 8.0
        _PatternSpeed("Pattern Speed", Range(0, 5)) = 0.4
        _PatternIntensity("Pattern Intensity", Range(0, 2)) = 0.25

        // Impact (per-shot ripple, multiple concentric rings)
        [HDR] _ImpactColor("Impact Color (HDR)", Color) = (1.0, 0.6, 0.2, 1.0)
        _ImpactBoost("Impact Boost", Range(0, 5)) = 2.5
        _ImpactWidth("Impact Wave Width", Range(0.05, 1.5)) = 0.18
        _ImpactSpeed("Impact Wave Speed", Range(0.1, 20)) = 5.0
        _ImpactLifetime("Impact Lifetime", Range(0.1, 4)) = 1.4
        _ImpactRingSpacing("Impact Ring Spacing", Range(0.1, 2)) = 0.45

        // Driven from script
        _ImpactPos("Impact Position (xyz=world, w=start time)", Vector) = (0,0,0,-1000)

        // Always-on slow scanner — visual cue that shield exists.
        [HDR] _AmbientColor("Ambient Scanner Color (HDR)", Color) = (0.9, 0.5, 0.2, 1.0)
        _AmbientSpeed("Ambient Speed", Range(0, 2)) = 0.35
        _AmbientWidth("Ambient Width", Range(0.02, 0.6)) = 0.10
        _AmbientIntensity("Ambient Intensity", Range(0, 3)) = 0.6

        // Heat / overload state
        [HDR] _HeatColor("Heat Color (HDR)", Color) = (1.0, 0.25, 0.1, 1.0)
        _Heat("Heat 0..1", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        LOD 100

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;

                float _PatternScale;
                float _PatternSpeed;
                float _PatternIntensity;

                float4 _ImpactColor;
                float _ImpactBoost;
                float _ImpactWidth;
                float _ImpactSpeed;
                float _ImpactLifetime;
                float _ImpactRingSpacing;

                float4 _ImpactPos;

                float4 _AmbientColor;
                float _AmbientSpeed;
                float _AmbientWidth;
                float _AmbientIntensity;

                float4 _HeatColor;
                float _Heat;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
            };

            float hash3(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float pattern(float3 wp)
            {
                float3 g = floor(wp);
                float3 f = frac(wp);
                float n = hash3(g);
                float edges = max(max(
                    smoothstep(0.92, 1.0, f.x) + smoothstep(0.08, 0.0, f.x),
                    smoothstep(0.92, 1.0, f.y) + smoothstep(0.08, 0.0, f.y)),
                    smoothstep(0.92, 1.0, f.z) + smoothstep(0.08, 0.0, f.z));
                return saturate(edges * 0.5 + n * 0.5);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs vni  = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = vpi.positionCS;
                OUT.positionWS  = vpi.positionWS;
                OUT.normalWS    = vni.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(vpi.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);
                float ndv = saturate(dot(N, V));
                float rim = pow(1.0 - ndv, _RimPower) * _RimIntensity;

                // Subtle scrolling pattern across the volume
                float t = _Time.y * _PatternSpeed;
                float3 wp = IN.positionWS * _PatternScale + float3(t, t * 0.7, t * 1.3);
                float pat = pattern(wp) * _PatternIntensity;

                // Per-shot impact ripple — three concentric rings expanding outward
                float ripple = 0.0;
                float age = _Time.y - _ImpactPos.w;
                if (age >= 0.0 && age < _ImpactLifetime)
                {
                    float d = distance(IN.positionWS, _ImpactPos.xyz);
                    float decay = 1.0 - saturate(age / _ImpactLifetime);
                    float waveR = age * _ImpactSpeed;

                    [unroll]
                    for (int i = 0; i < 3; i++)
                    {
                        float w = waveR - i * _ImpactRingSpacing;
                        if (w < 0.0) continue;
                        float band = exp(-pow((d - w) / max(0.0001, _ImpactWidth), 2.0));
                        ripple += band * (1.0 - i * 0.28);
                    }
                    ripple *= decay;
                }

                // Always-on slow scanner band that sweeps from south pole to north pole.
                // Uses world-space normal Y (latitude on sphere). Independent of impact.
                float ambSweep = fmod(_Time.y * _AmbientSpeed, 2.0) - 1.0; // -1..+1
                float ambBand = exp(-pow((N.y - ambSweep) / max(0.0001, _AmbientWidth), 2.0));
                float ambient = ambBand * _AmbientIntensity;

                // Heat tint — bias toward heat color as Heat increases.
                float heatBlend = smoothstep(0.0, 1.0, _Heat);
                float3 bodyRgb = lerp(_BaseColor.rgb, _HeatColor.rgb, heatBlend * 0.85);
                float3 rimRgb = lerp(_RimColor.rgb, _HeatColor.rgb, heatBlend * 0.9);

                // Heat pulse: starts pulsing visibly when heat > 0.4, peaks at 1.
                float pulseStrength = smoothstep(0.4, 1.0, _Heat);
                float heatPulse = (0.6 + 0.4 * sin(_Time.y * 9.0)) * pulseStrength;

                float3 col = bodyRgb * _BaseColor.a;
                col += rimRgb * rim;
                col += rimRgb * pat * 0.6;
                col += _AmbientColor.rgb * ambient;
                col += _ImpactColor.rgb * ripple * _ImpactBoost;
                col += _HeatColor.rgb * heatPulse * 2.0;
                col *= (1.0 + _Heat * 1.2);

                float alpha = saturate(_BaseColor.a + rim * 0.6 + pat * 0.4 + ripple + ambient * 0.5 + heatPulse * 0.4);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
