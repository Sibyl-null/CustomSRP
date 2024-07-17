#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#ifdef _DIRECTIONAL_PCF3
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif _DIRECTIONAL_PCF5
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif _DIRECTIONAL_PCF7
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

// https://docs.unity3d.com/cn/2022.3/Manual/SL-SamplerStates.html
#define SHADOW_SAMPLER sampler_linear_clamp_compare

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadeData[MAX_CASCADE_COUNT];         // x: 1 / cullingSphere.w^2, y: 阴影采样膨胀系数
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4 _ShadowAtlasSize;        // x: atlas width, y: 1f / atlas width(纹素大小)
    float4 _ShadowDistanceFade;
CBUFFER_END

struct ShadowMask
{
    bool distance;
    float4 shadows;
};

struct ShadowData
{
    int cascadeIndex;   // 阴影级联索引
    float cascadeBlend;
    float strength;
    ShadowMask shadowMask;
};

float FadeShadowStrength(float distance, float scale, float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    data.shadowMask.distance = false;
    data.shadowMask.shadows = 1.0;
    data.cascadeBlend = 1.0;
    data.strength = FadeShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);

    int index = 0;
    for (index = 0; index < _CascadeCount; ++index)
    {
        float4 sphere = _CascadeCullingSpheres[index];
        float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
        if (distanceSqr < sphere.w)
        {
            float fade = FadeShadowStrength(distanceSqr, _CascadeData[index].x, _ShadowDistanceFade.z);
            if (index == _CascadeCount - 1)
            {
                data.strength *= fade;
            }
            else
            {
                data.cascadeBlend = fade;    
            }
            break;
        }
    }

    // 超过最大级联数量，则不渲染阴影
    if (index == _CascadeCount)
    {
        data.strength = 0.0;
    }
    // 在当前级联和下一个级联之间进行抖动
#ifdef _CASCADE_BLEND_DITHER
    else if (data.cascadeBlend < surfaceWS.dither)
    {
        index += 1;
    }
#endif

    // 如果没有定义软级联混合，则将混合值设为 1，即不进行混合
#ifndef _CASCADE_BLEND_SOFT
    data.cascadeBlend = 1.0;
#endif
    
    data.cascadeIndex = index;
    return data;
}

struct DirectionalShadowData
{
    float strength;          // 阴影强度
    int tileIndex;           // 图集索引
    float normalBias;        // 法线偏移量
};

float SampleDirectionalShadowAtlas(float3 positionSTS)  // positionSTS: shadow texture space position
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterDirectionalShadow(float3 positionSTS)
{
#ifdef DIRECTIONAL_FILTER_SETUP
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.yyxx;
    DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);

    float shadow = 0;
    for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; ++i)
    {
        shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
    }
    return shadow;
#else
    return SampleDirectionalShadowAtlas(positionSTS);
#endif
}

float GetCascadedShadow(DirectionalShadowData directional, ShadowData global, Surface surfaceWS)
{
    float3 normalBias = surfaceWS.normal * directional.normalBias * _CascadeData[global.cascadeIndex].y;
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex], float4(surfaceWS.position + normalBias, 1)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);

    if (global.cascadeBlend < 1.0)
    {
        // 对下一个级联采样，并根据当前级联的混合权重进行插值
        normalBias = surfaceWS.normal * directional.normalBias * _CascadeData[global.cascadeIndex + 1].y;
        positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1], float4(surfaceWS.position + normalBias, 1)).xyz;
        shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
    }

    return  shadow;
}

float GetBakedShadow(ShadowMask mask)
{
    if (mask.distance)
        return mask.shadows.r;      // ShadowMask 贴图数据存储在 r 通道中

    return 1.0;
}

float GetBakedShadow(ShadowMask mask, float strength)
{
    if (mask.distance)
        return lerp(1.0, GetBakedShadow(mask), strength);

    return 1.0;
}

float MixBakedAndRealtimeShadows(ShadowData global, float shadow, float strength)
{
    float baked = GetBakedShadow(global.shadowMask);
    if (global.shadowMask.distance)
    {
        shadow = lerp(baked, shadow, global.strength);
        return lerp(1.0, shadow, strength);
    }

    return lerp(1.0, shadow, strength * global.strength);
}

/** 返回因阴影造成的光照衰减系数 */
float GetDirectionalShadowAttenuation(DirectionalShadowData directional, ShadowData global, Surface surfaceWS)
{
#ifndef _RECEIVE_SHADOWS
    return 1.0;
#endif
    
    if (directional.strength * global.strength <= 0.0)
        return GetBakedShadow(global.shadowMask, abs(directional.strength));

    float shadow = GetCascadedShadow(directional, global, surfaceWS);
    shadow = MixBakedAndRealtimeShadows(global, shadow, directional.strength);
    return shadow;
}

#endif // CUSTOM_SHADOWS_INCLUDED