#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDED

struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ShadowCasterPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);

    // 阴影平坠裁剪问题解决（将顶点位置固定到近平面）
    #if UNITY_REVERSED_Z
        output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
        output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif

    output.baseUV = TransformBaseUV(input.baseUV);
    
    return output;
}

// 不需要输出颜色
void ShadowCasterPassFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    ClipLOD(input.positionCS, unity_LODFade.x);
    float4 base = GetBase(input.baseUV);
    
#ifdef _SHADOWS_CLIP
    clip(base.a - GetCutoff(input.baseUV));
#elif _SHADOWS_DITHER
    float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
    clip(base.a - dither);
#endif
}

#endif // CUSTOM_SHADOW_CASTER_PASS_INCLUDED