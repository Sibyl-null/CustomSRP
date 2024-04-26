#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04

struct BRDF
{
    float3 diffuse;          // 发散颜色
    float3 specular;         // 反射颜色
    float roughness;         // 粗糙度
};

/** 计算发散率，1 - 发射率，能量守恒 */
float OneMinusReflectivity(float metallic)
{
    float range = 1 - MIN_REFLECTIVITY;
    return (1 - metallic) * range;
}

BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false)
{
    BRDF brdf;
    
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusReflectivity;
    if (applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;   // alpha 预乘
    }
    
    brdf.specular = lerp(MIN_REFLECTIVITY * 1.0, surface.color, surface.metallic);;

    // 知觉光滑度 -> 知觉粗糙度
    float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    // 知觉粗糙度 -> 真实粗糙度
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    return brdf;
}

/** 计算高光强度 */
float SpecularStrength(Surface surface, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Square(saturate(dot(surface.normal, h)));
    float lh2 = Square(saturate(dot(light.direction, h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return SpecularStrength(surface,brdf, light) * brdf.specular + brdf.diffuse;
}

#endif // CUSTOM_BRDF_INCLUDED