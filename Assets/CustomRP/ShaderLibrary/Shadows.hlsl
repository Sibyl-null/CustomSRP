#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

// https://docs.unity3d.com/cn/2022.3/Manual/SL-SamplerStates.html
#define SHADOW_SAMPLER sampler_linear_clamp_compare

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4 _ShadowDistanceFade;
CBUFFER_END

struct ShadowData
{
    int cascadeIndex;   // 阴影级联索引
    float strength;
};

float FadeShadowStrength(float distance, float scale, float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    data.strength = FadeShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);

    int index = 0;
    for (index = 0; index < _CascadeCount; ++index)
    {
        float4 sphere = _CascadeCullingSpheres[index];
        float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
        if (distanceSqr < sphere.w)
        {
            if (index == _CascadeCount - 1)
            {
                data.strength *= FadeShadowStrength(distanceSqr, 1.0 / sphere.w, _ShadowDistanceFade.z);
            }
            break;
        }
    }

    if (index == _CascadeCount)
        data.strength = 0.0;
    
    data.cascadeIndex = index;
    return data;
}

struct DirectionalShadowData
{
    float strength;     // 阴影强度
    int tileIndex;      // 图集索引
};

float SampleDirectionalShadowAtlas(float3 positionSTS)  // positionSTS: shadow texture space position
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

/** 返回因阴影造成的光照衰减系数 */
float GetDirectionalShadowAttenuation(DirectionalShadowData data, Surface surfaceWS)
{
    if (data.strength <= 0.0)
        return 1.0;
    
    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surfaceWS.position, 1)).xyz;
    float shadow = SampleDirectionalShadowAtlas(positionSTS);

    // 出于艺术考量或表示半透明表面的阴影，灯光的阴影强度可以被降低
    return lerp(1.0, shadow, data.strength);
}

#endif // CUSTOM_SHADOWS_INCLUDED