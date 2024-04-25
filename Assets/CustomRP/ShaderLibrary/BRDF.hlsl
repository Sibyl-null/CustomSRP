#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

struct BRDF
{
    float3 diffuse;          // 漫反射颜色
    float3 specular;         // 镜面反射颜色
    float roughness;         // 粗糙度
};

BRDF GetBRDF(Surface surface)
{
    BRDF brdf;
    brdf.diffuse = surface.color;
    brdf.specular = 0;
    brdf.roughness = 1;
    return brdf;
}

#endif // CUSTOM_BRDF_INCLUDED