#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

struct Light
{
    float3 color;
    float3 direction;    // 光从哪里来
};

Light GetDirectionalLight()
{
    Light light;
    light.color = float3(1, 1, 1);
    light.direction = float3(0, 1, 0);
    return light;
}

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

#endif // CUSTOM_LIGHT_INCLUDED