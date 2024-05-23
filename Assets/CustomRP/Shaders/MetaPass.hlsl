#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float2 lightMapUV : TEXCOORD1;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
};

Varyings MetaPassVertex(Attributes input)
{
    Varyings output;

    // TODO: 不理解这是在干什么？
    input.positionOS.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    output.positionCS = TransformWorldToHClip(input.positionOS);
    output.positionCS.z = input.positionOS.z > 0.0 ? FLT_INF : 0.0;
    
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 MetaPassFragment(Varyings input) : SV_TARGET
{
    float4 base = GetBase(input.baseUV);

    Surface surface;
    ZERO_INITIALIZE(Surface, surface);
    surface.color = base.rgb;
    surface.metallic = GetMetallic(input.baseUV);
    surface.smoothness = GetSmoothness(input.baseUV);
    
    BRDF brdf = GetBRDF(surface);
    float4 meta = 0.0;
    return meta;
}

#endif // CUSTOM_META_PASS_INCLUDED