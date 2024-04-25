#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

/** 光照颜色计算 */
float3 IncomingLight(Surface surface, Light light)
{
    // 漫反射项
    return saturate(dot(surface.normal, light.direction)) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    return IncomingLight(surface, light) * brdf.diffuse;
}

float3 GetLighting(Surface surface, BRDF brdf)
{
    float3 color = 0;
    for (int i = 0; i < GetDirectionalLightCount(); ++i)
    {
        color += GetLighting(surface, brdf, GetDirectionalLight(i));
    }
    return color;
}

#endif // CUSTOM_LIGHTING_INCLUDED