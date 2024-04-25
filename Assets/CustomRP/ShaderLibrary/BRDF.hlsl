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

BRDF GetBRDF(Surface surface)
{
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    
    BRDF brdf;
    brdf.diffuse = surface.color * oneMinusReflectivity;
    brdf.specular = 0;
    brdf.roughness = 1;
    return brdf;
}

#endif // CUSTOM_BRDF_INCLUDED