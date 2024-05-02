#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

// https://docs.unity3d.com/cn/2022.3/Manual/SL-SamplerStates.html
#define SHADOW_SAMPLER sampler_linear_clamp_compare

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
CBUFFER_END

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