#ifndef CUSTOM_GI_INCLUDED
#define CUSTOM_GI_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

TEXTURE2D(unity_Lightmap);
SAMPLER(sampler_unity_Lightmap);

TEXTURE2D(unity_ShadowMask);
SAMPLER(sampler_unity_ShadowMask);

TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(sampler_unity_ProbeVolumeSH);

#ifdef LIGHTMAP_ON
    #define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
    #define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;
    #define TRANSFER_GI_DATA(input, output) output.lightMapUV = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    #define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
    #define GI_ATTRIBUTE_DATA
    #define GI_VARYINGS_DATA
    #define TRANSFER_GI_DATA(input, output)
    #define GI_FRAGMENT_DATA(input) 0.0
#endif // LIGHTMAP_ON


struct GI
{
    float3 diffuse;
    ShadowMask shadowMask;
};

float3 SampleLightProbe(Surface surfaceWS)
{
#ifdef LIGHTMAP_ON
    return 0;
#else
    if (unity_ProbeVolumeParams.x)
    {
        return SampleProbeVolumeSH4(
                TEXTURE3D_ARGS(unity_ProbeVolumeSH, sampler_unity_ProbeVolumeSH),
                surfaceWS.position, surfaceWS.normal,
                unity_ProbeVolumeWorldToObject,
                unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
                unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
            );
    }
    else
    {
        float4 coefficients[7];
        coefficients[0] = unity_SHAr;
        coefficients[1] = unity_SHAg;
        coefficients[2] = unity_SHAb;
        coefficients[3] = unity_SHBr;
        coefficients[4] = unity_SHBg;
        coefficients[5] = unity_SHBb;
        coefficients[6] = unity_SHC;
        return max(0.0, SampleSH9(coefficients, surfaceWS.normal));
    }
#endif // LIGHTMAP_ON
}

float3 SampleLightMap(float2 lightMapUV)
{
#ifdef LIGHTMAP_ON
    #ifdef UNITY_LIGHTMAP_FULL_HDR
        bool compress = false;
    #else
        bool compress = true;
    #endif
    
    return SampleSingleLightmap(
        TEXTURE2D_ARGS(unity_Lightmap, sampler_unity_Lightmap),
        lightMapUV,
        float4(1, 1, 0, 0),
        compress,
        float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0, 0));
#else
    return 0.0;
#endif // LIGHTMAP_ON
}

float4 SampleBakedShadows(float2 lightMapUV)
{
#ifdef LIGHTMAP_ON
    return SAMPLE_TEXTURE2D(unity_ShadowMask, sampler_unity_ShadowMask, lightMapUV);
#else
    return 1.0;
#endif
}

GI GetGI(float2 lightMapUV, Surface surfaceWS)
{
    GI gi;
    gi.diffuse = SampleLightMap(lightMapUV) + SampleLightProbe(surfaceWS);
    gi.shadowMask.distance = false;
    gi.shadowMask.shadows = 1.0;

#ifdef _SHADOW_MASK_DISTANCE
    // 这使 distance 的值成为编译时常量，使用该值并不会导致动态分支
    gi.shadowMask.distance = true;
    gi.shadowMask.shadows = SampleBakedShadows(lightMapUV);
#endif
    return gi;
}

#endif // CUSTOM_GI_INCLUDED