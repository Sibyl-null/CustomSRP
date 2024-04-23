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

#endif // CUSTOM_LIGHT_INCLUDED