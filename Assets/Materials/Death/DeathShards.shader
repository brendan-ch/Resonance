Shader "Resonance/DeathShards"
{
    Properties
    {
        _Color ("Color", Color) = (0, 1, 1, 1)
        _EmissionColor ("Emission Color", Color) = (0, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 5.0
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _FragmentationAmount ("Fragmentation", Range(0, 1)) = 0
        _NoiseScale ("Noise Scale", Float) = 5.0
        _ShardDensity ("Shard Density", Range(0, 1)) = 0.15
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Blend SrcAlpha One // Additive blending for glow
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct v2g
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };
            
            struct g2f
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float dissolve : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _DissolveAmount;
                float _FragmentationAmount;
                float _NoiseScale;
                float _ShardDensity;
            CBUFFER_END

            float noise3D(float3 p)
            {
                return frac(sin(dot(p, float3(12.9898, 78.233, 45.164))) * 43758.5453);
            }

            v2g vert(Attributes IN)
            {
                v2g OUT;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);
                
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;
                
                return OUT;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {
                float3 center = (IN[0].positionWS + IN[1].positionWS + IN[2].positionWS) / 3.0;
                float3 normal = normalize(IN[0].normalWS + IN[1].normalWS + IN[2].normalWS);
                
                // Better culling - use multiple noise samples for more variation
                float cullNoise1 = noise3D(center * 137.5);
                float cullNoise2 = noise3D(center * 241.3 + float3(17.3, 42.1, 89.7));
                float cullValue = (cullNoise1 + cullNoise2) * 0.5;
                
                if (cullValue > _ShardDensity)
                {
                    return; // Cull this triangle
                }
                
                float noiseValue = noise3D(center * _NoiseScale);
                float dissolveThreshold = _DissolveAmount + noiseValue * 0.3 - 0.15;
                
                // Float upward with slight drift
                float3 upwardDir = float3(
                    (noise3D(center * 8.5) - 0.5) * 0.4,
                    1.0,
                    (noise3D(center * 6.7) - 0.5) * 0.4
                );
                upwardDir = normalize(upwardDir);
                
                float speed = (noiseValue * 0.3 + 0.7) * 1.2;
                float3 movementOffset = upwardDir * _FragmentationAmount * speed;
                
                // Gentle rotation
                float angle = noiseValue * 3.14159 * _FragmentationAmount * 0.4;
                float3 rotAxis = normalize(float3(
                    noise3D(center * 8.2) - 0.5,
                    noise3D(center * 6.4) - 0.5,
                    noise3D(center * 7.8) - 0.5
                ));
                
                g2f OUT;
                
                for(int i = 0; i < 3; i++)
                {
                    float3 localPos = IN[i].positionWS - center;
                    
                    // Rotation
                    float cosAngle = cos(angle);
                    float sinAngle = sin(angle);
                    float3 rotatedPos = localPos * cosAngle + 
                                       cross(rotAxis, localPos) * sinAngle + 
                                       rotAxis * dot(rotAxis, localPos) * (1.0 - cosAngle);
                    
                    float3 finalPos = center + rotatedPos + movementOffset;
                    OUT.positionCS = TransformWorldToHClip(finalPos);
                    OUT.normalWS = IN[i].normalWS;
                    OUT.worldPos = IN[i].positionWS;
                    OUT.dissolve = dissolveThreshold;
                    
                    triStream.Append(OUT);
                }
            }

            half4 frag(g2f IN) : SV_Target
            {
                float localNoise = noise3D(IN.worldPos * _NoiseScale);
                
                // Dissolve check
                float fadeStart = 0.85;
                if(IN.dissolve > fadeStart)
                {
                    float fadeAmount = (IN.dissolve - fadeStart) / (1.0 - fadeStart);
                    if(localNoise < fadeAmount)
                        discard;
                }
                
                // Simple lighting
                float3 normalWS = normalize(IN.normalWS);
                float ndotl = saturate(dot(normalWS, float3(0, 1, 0))) * 0.5 + 0.5;
                
                // Bright glowing color
                float3 color = _Color.rgb * ndotl;
                float3 emission = _EmissionColor.rgb * _EmissionIntensity;
                
                float3 finalColor = color + emission;
                
                // Fade alpha at the end
                float alpha = _Color.a;
                if(IN.dissolve > fadeStart)
                {
                    float dissolveFade = 1.0 - smoothstep(fadeStart, 1.0, IN.dissolve);
                    alpha *= dissolveFade;
                }
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    
    Fallback "Universal Render Pipeline/Unlit"
}
