#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

/** 光照颜色计算 */
float3 IncomingLight(Surface surface, Light light)
{
    // 漫反射项
    return saturate(dot(surface.normal, light.direction)) * light.color;
}

float3 GetLighting(Surface surface, Light light)
{
    return IncomingLight(surface, light) * surface.color;
}

float3 GetLighting(Surface surface)
{
    return GetLighting(surface, GetDirectionalLight());
}

#endif // CUSTOM_LIGHTING_INCLUDED