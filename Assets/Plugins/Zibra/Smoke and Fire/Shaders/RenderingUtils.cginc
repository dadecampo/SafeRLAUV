#define TWO_PI 6.28318530718
#define PI 3.14159265359
#define FAR_DISTANCE 1e5
#define RENDERING_EPS 1.0e-4
#define DIVISION_EPS 1.0e-9

#define MAX_LIGHT_COUNT 16

#define SIMULATION_MODE_SMOKE 0
#define SIMULATION_MODE_COLORED_SMOKE 1
#define SIMULATION_MODE_FIRE 2

int SimulationMode;

float4 LightColorArray[MAX_LIGHT_COUNT];
float4 LightPositionArray[MAX_LIGHT_COUNT];
int LightCount;
int MainLightMode;

Texture2D<float4> BlueNoise;

Texture3D<float> Density;
SamplerState samplerDensity;
int DensityDownscale;

Texture3D<float2> Color;
SamplerState samplerColor;

Texture3D<float3> Illumination;
SamplerState samplerIllumination;

Texture3D<float> Shadowmap;
SamplerState samplerShadowmap;

Texture3D<float4> Lightmap;
SamplerState samplerLightmap;

int ShadowDepth;
int PrimaryShadows;
int IlluminationShadows;

float ShadowStepSize;
int ShadowMaxSteps;

float4 DitherValues;

float3 GridSize;
float3 ShadowGridSize;
float3 LightGridSize;
float3 ContainerScale;
float3 ContainerPosition;

float3 LightColor;
float3 LightDirWorld;

float FireBrightness;
float BlackBodyBrightness;
float4 FireColor;

float4 AbsorptionColor;
float4 ScatteringColor;
float4 ShadowColor;
float ScatteringAttenuation;
float ScatteringContribution;
float ScatteringPhaseAttenuation;

float IlluminationSoftness;
float FakeShadows;
float ShadowDistanceDecay;
float ShadowIntensity;
float StepSize;
float ShadowDepthSharpness;

float SmokeDensity;

// Fluid material parameters, see SetMaterialParams()
float2 OriginalCameraResolution;
float4x4 ViewProjectionInverse;

float3 Texel2UVW(float3 p, float3 size)
{
    return (p + 0.5) / size;
}

float3 BlackBodyRadiation(float temperature)
{
    float3 output = 0.0;
    
    const float saturation = 1.0;
    [unroll]
    for (float i = 0.; i < 3.; i++) // +=.1 if you want to better sample the spectrum.
    { 
        float freq = 1. + .5 * i;
        output[int(i)] += 10. / saturation * (freq * freq * freq) / (exp((freq / temperature)) - 1.); // Planck law
    }
    
    return output;
}

float3 RGB2Density(float3 rgb)
{
    return clamp(float3(rgb.x + rgb.y + rgb.z, rgb.y - rgb.x, rgb.z - rgb.y), -1.0, 1.0);
}

float3 Density2RGB(float3 d)
{
    return (1.0 / 3.0) * clamp(d.x + float3(-2.0 * d.y - d.z, d.y - d.z, d.y + 2 * d.z), 0.0, 1.0);
}

float3 BoxIntersection(float3 ro, float3 rd, float3 minpos, float3 maxpos)
{
    float3 inverse_dir = 1.0 / rd;
    float3 tbot = inverse_dir * (minpos - ro);
    float3 ttop = inverse_dir * (maxpos - ro);
    float3 tmin = min(ttop, tbot);
    float3 tmax = max(ttop, tbot);
    float2 traverse = max(tmin.xx, tmin.yz);
    float traverselow = max(traverse.x, traverse.y);
    traverse = min(tmax.xx, tmax.yz);
    float traversehi = min(traverse.x, traverse.y);
    return float3(float(traversehi > max(traverselow, 0.0)), traverselow, traversehi);
}

float sdBoxInside(float3 p, float3 b)
{
    float3 q = abs(p) - b;
    return min(max(q.x, max(q.y, q.z)), 0.0);
}

float sqr(float x)
{
    return x * x;
}

float3 sqr(float3 x)
{
    return x * x;
}

float Sum(float4 x)
{
    return x.x + x.y + x.z + x.w;
}

float Sum(float3 x)
{
    return x.x + x.y + x.z;
}

float Max(float3 col)
{
    return max(col.x, max(col.y, col.z));
}

float3 WorldToUVW(float3 p)
{
    return (p - (ContainerPosition - ContainerScale * 0.5)) / ContainerScale + 0.5/GridSize;
}

float3 GetNodeF(float3 p)
{
    return GridSize * WorldToUVW(p) - 0.5;
}

int GetNodeID(int3 node)
{
    node = clamp(node, int3(0, 0, 0), int3(GridSize) - int3(1, 1, 1));
    return node.x + node.y * GridSize.x +
            node.z * GridSize.x * GridSize.y;
}

int GetNodeID(float3 node)
{
    return GetNodeID(int3(node));
}

float3 Simulation2World(float3 pos)
{
    return ContainerScale * pos / GridSize + (ContainerPosition - ContainerScale * 0.5);
}

bool insideGrid(float3 pos)
{
    float3 Size = ContainerScale * 0.5 + 0.01*ContainerScale / GridSize;
    return all(pos > ContainerPosition - Size) && all(pos < ContainerPosition + Size);
}

float3 getNode(int ID)
{
    uint3 S = GridSize;
    return float3(ID % S.x, (ID / S.x) % S.y, ID / (S.x * S.y));
}

struct RayProperties
{
    float3 absorption;
    float3 incoming;
};

float4 cubic(float v)
{
    float4 n = float4(1.0, 2.0, 3.0, 4.0) - v;
    float4 s = n * n * n;
    float x = s.x;
    float y = s.y - 4.0 * s.x;
    float z = s.z - 4.0 * s.y + 6.0 * s.x;
    float w = 6.0 - x - y - z;
    return float4(x, y, z, w) * (1.0 / 6.0);
}

float TricubicSample(Texture3D<float> Texture, SamplerState TextureSampler, float3 texCoords)
{
    int3 TextureSize;
    Texture.GetDimensions(TextureSize.x, TextureSize.y, TextureSize.z);

    float3 texSize = TextureSize;
    float3 invTexSize = 1.0 / texSize;

    texCoords = texCoords * texSize - 0.5;

    float3 fxy = frac(texCoords);
    texCoords -= fxy;

    float4 xcubic = cubic(fxy.x);
    float4 ycubic = cubic(fxy.y);
    float4 zcubic = cubic(fxy.z);

    float2 cx = texCoords.x + float2(-0.5, +1.5);
    float2 cy = texCoords.y + float2(-0.5, +1.5);
    float2 cz = texCoords.z + float2(-0.5, +1.5);

    float2 sx = xcubic.xz + xcubic.yw;
    float2 sy = ycubic.xz + ycubic.yw;
    float2 sz = zcubic.xz + zcubic.yw;

    float2 offsetx = invTexSize.x * (cx + xcubic.yw / sx);
    float2 offsety = invTexSize.y * (cy + ycubic.yw / sy);
    float2 offsetz = invTexSize.z * (cz + zcubic.yw / sz);

    float sample0 = Texture.SampleLevel(TextureSampler, float3(offsetx.x, offsety.x, offsetz.x), 0);
    float sample1 = Texture.SampleLevel(TextureSampler, float3(offsetx.y, offsety.x, offsetz.x), 0);
    float sample2 = Texture.SampleLevel(TextureSampler, float3(offsetx.x, offsety.y, offsetz.x), 0);
    float sample3 = Texture.SampleLevel(TextureSampler, float3(offsetx.y, offsety.y, offsetz.x), 0);
    float sample4 = Texture.SampleLevel(TextureSampler, float3(offsetx.x, offsety.x, offsetz.y), 0);
    float sample5 = Texture.SampleLevel(TextureSampler, float3(offsetx.y, offsety.x, offsetz.y), 0);
    float sample6 = Texture.SampleLevel(TextureSampler, float3(offsetx.x, offsety.y, offsetz.y), 0);
    float sample7 = Texture.SampleLevel(TextureSampler, float3(offsetx.y, offsety.y, offsetz.y), 0);

    float px = sx.x / (sx.x + sx.y);
    float py = sy.x / (sy.x + sy.y);
    float pz = sz.x / (sz.x + sz.y);

    float sample0xy = lerp(lerp(sample3, sample2, px), lerp(sample1, sample0, px), py);
    float sample1xy = lerp(lerp(sample7, sample6, px), lerp(sample5, sample4, px), py);
    return lerp(sample1xy, sample0xy, pz);
}

float4 CubicHermite(float x)
{
    float x2 = x * x;
    float x3 = x2 * x;

    return float4(-0.5 * x3 + x2 - 0.5 * x, 1.5 * x3 - 2.5 * x2 + 1.0,
                  -1.5 * x3 + 2.0 * x2 + 0.5 * x, 0.5 * x3 - 0.5 * x2);
}

float SampleDensityNearest(float3 pos)
{
    return Density[GetNodeF(pos)];
}

float SampleDensityLinear(float3 pos)
{
    return Density.SampleLevel(samplerDensity, WorldToUVW(pos), 0);
}

float SampleDensityTricubic(float3 pos)
{
    return TricubicSample(Density, samplerDensity, WorldToUVW(pos));
}

float2 SampleColorLinear(float3 pos)
{
    return Color.SampleLevel(samplerColor, WorldToUVW(pos), 0);
}

float3 SampleIlluminationLinear(float3 pos)
{
    return Illumination.SampleLevel(samplerIllumination, WorldToUVW(pos), 0);
}

float HenyeyGreenstein(float g, float costh)
{
    return (1.0 - g * g) / (pow(1.0 + g * g - 2.0 * g * costh, 3.0 / 2.0));
}

float PhaseFunction(float g, float costh)
{
    return lerp(HenyeyGreenstein(g, costh), HenyeyGreenstein(g, -costh), 0.25);
}

void IntegrateAbsorptionScattering(inout RayProperties prop, float opticalDensity, float3 illumination)
{
    float3 extinction = exp(-AbsorptionColor.xyz * opticalDensity);

    float3 emissColor = illumination; // environment light
    float3 S = emissColor; // incoming light
    float3 Sint = S * (1.0 - extinction); // integrate along the current step segment

    prop.incoming += Sint * prop.absorption;
    prop.absorption *= extinction;
}

bool SimulationContainerIntersection(inout float3 rayOrigin, float3 rayDirection, out float distanceToBox, out float intersectionThickness, float3 scale = 1.0, float maxDistance = FAR_DISTANCE)
{
    float3 t0 = BoxIntersection(rayOrigin, rayDirection, ContainerPosition - ContainerScale * 0.5 * scale, ContainerPosition + ContainerScale * 0.5 * scale);

    distanceToBox = FAR_DISTANCE;
    intersectionThickness = 0.0;
    
    if (t0.z < 0.0 || (t0.y > maxDistance) || t0.z <= t0.y)
        return false;
   
    if (t0.y >= 0.0)
        rayOrigin += rayDirection * t0.y;
    
    distanceToBox = max(t0.y, 0.0);
    intersectionThickness = t0.z - t0.y;
    
    return true;
}

float SampleShadowmap(float3 rayOrigin, float simulationScale)
{
    float3 boxPos = rayOrigin;
    float distance, thickness;
    if (!SimulationContainerIntersection(boxPos, LightDirWorld, distance, thickness))
        return 0.0;

    int3 ShadowmapSize;
    Shadowmap.GetDimensions(ShadowmapSize.x, ShadowmapSize.y, ShadowmapSize.z);

    float3 normBoxPos = (boxPos - ContainerPosition) / ContainerScale + 0.5 / float3(ShadowmapSize);
    float OpticalDensity = Shadowmap.SampleLevel(samplerShadowmap, normBoxPos + 0.5, 0);
   
    return ShadowIntensity * OpticalDensity *
           exp(-ShadowDistanceDecay * max(distance / simulationScale - 1.0, 0.0));
}

float SampleShadowmapSmooth(float3 rayOrigin, float simulationScale)
{
    float3 boxPos = rayOrigin;
    float distance, thickness;
    if (!SimulationContainerIntersection(boxPos, LightDirWorld, distance, thickness))
        return 0.0;

    int3 ShadowmapSize;
    Shadowmap.GetDimensions(ShadowmapSize.x, ShadowmapSize.y, ShadowmapSize.z);

    float3 normBoxPos = (boxPos - ContainerPosition) / ContainerScale + 0.5 / float3(ShadowmapSize);
    float OpticalDensity = Shadowmap.SampleLevel(samplerShadowmap, normBoxPos + 0.5, 0);
    if (OpticalDensity < 5.0 || OpticalDensity > 1e-4)
        OpticalDensity = TricubicSample(Shadowmap, samplerShadowmap, normBoxPos + 0.5);
    
    return ShadowIntensity * OpticalDensity *
           exp(-ShadowDistanceDecay * max(distance / simulationScale - 1.0, 0.0));
}

float4 LightmapSample(float3 texCoords)
{
    int3 TextureSize;
    Lightmap.GetDimensions(TextureSize.x, TextureSize.y, TextureSize.z);
    
    float3 texSize = TextureSize;
    float3 invTexSize = 1.0 / texSize;

    texCoords = texCoords * texSize - 0.5;

    float3 fxy = frac(texCoords);
    texCoords -= fxy;

    float4 xcubic = cubic(fxy.x);
    float4 ycubic = cubic(fxy.y);
    float4 zcubic = cubic(fxy.z);

    float2 cx = texCoords.x + float2(-0.5, +1.5);
    float2 cy = texCoords.y + float2(-0.5, +1.5);
    float2 cz = texCoords.z + float2(-0.5, +1.5);

    float2 sx = xcubic.xz + xcubic.yw;
    float2 sy = ycubic.xz + ycubic.yw;
    float2 sz = zcubic.xz + zcubic.yw;

    float2 offsetx = invTexSize.x * (cx + xcubic.yw / sx);
    float2 offsety = invTexSize.y * (cy + ycubic.yw / sy);
    float2 offsetz = invTexSize.z * (cz + zcubic.yw / sz);

    float4 sample0 = Lightmap.SampleLevel(samplerLightmap, float3(offsetx.x, offsety.x, offsetz.x), 0);
    float4 sample1 = Lightmap.SampleLevel(samplerLightmap, float3(offsetx.y, offsety.x, offsetz.x), 0);
    float4 sample2 = Lightmap.SampleLevel(samplerLightmap, float3(offsetx.x, offsety.y, offsetz.x), 0);
    float4 sample3 = Lightmap.SampleLevel(samplerLightmap, float3(offsetx.y, offsety.y, offsetz.x), 0);
    float4 sample4 = Lightmap.SampleLevel(samplerLightmap, float3(offsetx.x, offsety.x, offsetz.y), 0);
    float4 sample5 = Lightmap.SampleLevel(samplerLightmap, float3(offsetx.y, offsety.x, offsetz.y), 0);
    float4 sample6 = Lightmap.SampleLevel(samplerLightmap, float3(offsetx.x, offsety.y, offsetz.y), 0);
    float4 sample7 = Lightmap.SampleLevel(samplerLightmap, float3(offsetx.y, offsety.y, offsetz.y), 0);

    float px = sx.x / (sx.x + sx.y);
    float py = sy.x / (sy.x + sy.y);
    float pz = sz.x / (sz.x + sz.y);

    float4 sample0xy = lerp(lerp(sample3, sample2, px), lerp(sample1, sample0, px), py);
    float4 sample1xy = lerp(lerp(sample7, sample6, px), lerp(sample5, sample4, px), py);
    return lerp(sample1xy, sample0xy, pz);
}

float3 ShadowedScattering(float3 OpticalDepth)
{
    OpticalDepth *= ShadowColor.rgb;
    
    // fake multiple scattering nonsense, looks good
    float3 luminance = 0.0;
    float a = 1.0;
    float b = 1.0;
    float c = 1.0;

    for (int n = 0; n < 5; n++)
    {
        luminance += b * exp(-OpticalDepth * a);
        a *= ScatteringAttenuation;
        b *= ScatteringContribution;
        c *= (1.0 - ScatteringPhaseAttenuation);
    }
    return ScatteringColor.xyz * luminance;
}

float2 GetLightAttenuation(float dist, float range)
{
    float distanceSqr = pow(max(dist * dist, RENDERING_EPS), 1.0 / (4.0 * IlluminationSoftness));
    float rangeAttenuation = sqr(
		saturate(1.0 - sqr(distanceSqr * range))
	);
    return float2(max(rangeAttenuation / (distanceSqr + 1.0), RENDERING_EPS), rangeAttenuation);
}

float GetLightmapShadow(float3 samplingPos)
{
    if (LightCount == 0)
        return 1.0;
    
    float simulationScale =
        (1.0f / 3.0f) * (ContainerScale.x + ContainerScale.y + ContainerScale.z);
    
    int3 TextureSize;
    Lightmap.GetDimensions(TextureSize.x, TextureSize.y, TextureSize.z);
    
    float3 textureScale = 1.0 - 1.0 / TextureSize;
    
    float2 averageShadow = RENDERING_EPS;
    for (int i = 0; i < LightCount; i++)
    {
        float3 boxPos = samplingPos;
        float dist = distance(LightPositionArray[i].xyz, samplingPos);
        float3 dir = (LightPositionArray[i].xyz - samplingPos) / dist;
        float distance, thickness;
        float2 attenuation = GetLightAttenuation(dist, LightPositionArray[i].w);
        float3 illumination = LightColorArray[i].xyz * attenuation.x;
        if (!SimulationContainerIntersection(boxPos, dir, distance, thickness, textureScale, dist))
        { 
            //the ray doesn't hit the volume
            averageShadow += Sum(illumination);
        }
        else
        {
            float3 texCoords = (boxPos - ContainerPosition) / ContainerScale + 0.5;
            float weight = exp(-ShadowDistanceDecay * max(distance / simulationScale, 0.0) - 0.1 / (thickness / simulationScale + RENDERING_EPS));
            //weight *= attenuation.y;
            averageShadow += float2(lerp(1.0, LightmapSample(texCoords).w, weight), 1.0) * Sum(illumination);
        }
    }
    
    return averageShadow.x/averageShadow.y;
}

float TraceShadow(float3 rayOrigin, float3 rayDirection, float dt, float stepScale, float maxDistance, out float edgeDist)
{
    float3 t0 = BoxIntersection(rayOrigin, rayDirection, ContainerPosition - ContainerScale * 0.5,
                                ContainerPosition + ContainerScale * 0.5);

    edgeDist = max(t0.z, 0.0);
    float opticalDepth = 0.0;
    float t = max(t0.y, 0.0);
    float tmax = min(maxDistance, max(t0.z, 0.0));
    stepScale *= DensityDownscale;
    dt *= DensityDownscale;
    for (int i = 0; i < ShadowMaxSteps; i++)
    {
        t += dt;

        if (t >= tmax || opticalDepth > 30.0)
            break;

        float3 cpos = rayOrigin + t * rayDirection;

        float rho = SampleDensityLinear(cpos);
            
        opticalDepth += rho * stepScale;
    }

    return opticalDepth;
}

bool TraceRay(float3 rayOrigin, float3 rayDirection, float tMax, inout RayProperties prop)
{
    float3 t0 = BoxIntersection(rayOrigin, rayDirection, ContainerPosition - ContainerScale * 0.5,
                                ContainerPosition + ContainerScale * 0.5);

    if (t0.x < 0.5) // missed grid
        return false;

    float3 intersectionPoint = rayOrigin + t0.y * rayDirection;

    float simulationScale =
        (1.0f / 3.0f) * (ContainerScale.x + ContainerScale.y + ContainerScale.z);
    float cellSize = ContainerScale.x / GridSize.x;
    
    float t = max(t0.y, 0.0);
    for (int i = 0; i < 256; i++)
    {
        float dt = StepSize * cellSize;
        float stepScale = dt / simulationScale;
        float t1 = dt * DitherValues.x + t; 

        t += dt;
        if (t1 >= min(t0.z, tMax) || Sum(prop.absorption) < 1e-2) 
            break;

        float3 cpos = rayOrigin + t1 * rayDirection;

        float rho = SampleDensityLinear(cpos);

        if (rho < 1e-4) 
            continue;
        
        float3 col = SampleIlluminationLinear(cpos);
        IntegrateAbsorptionScattering(prop, stepScale * rho, col);
    }

    return true;
}

float4x4 inverse(float4x4 m) {
    float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
    float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
    float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
    float n41 = m[0][3], n42 = m[1][3], n43 = m[2][3], n44 = m[3][3];

    float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
    float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
    float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
    float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;

    float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
    float idet = 1.0f / det;

    float4x4 ret;

    ret[0][0] = t11 * idet;
    ret[0][1] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * idet;
    ret[0][2] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * idet;
    ret[0][3] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * idet;

    ret[1][0] = t12 * idet;
    ret[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * idet;
    ret[1][2] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * idet;
    ret[1][3] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * idet;

    ret[2][0] = t13 * idet;
    ret[2][1] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * idet;
    ret[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * idet;
    ret[2][3] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * idet;

    ret[3][0] = t14 * idet;
    ret[3][1] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * idet;
    ret[3][2] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * idet;
    ret[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * idet;

    return ret;
}

float3 ClipToWorld(float4 clipPos, float4x4 inverseVP)
{
    float4 worldPos = mul(inverseVP, clipPos);
    return worldPos.xyz / worldPos.w;
}

void GetCameraRay(float2 uv, out float3 rayOrigin, out float3 rayEnd, in float4x4 inverseVP)
{
    float2 c = float2(2.0f * uv.x - 1.0f, -2.0f * uv.y + 1.0f);
    //works for orthographic and perspective cameras
    rayOrigin = ClipToWorld(float4(c, 1.0, 1.0), inverseVP); //near plane
    rayEnd = ClipToWorld(float4(c, 0.0, 1.0), inverseVP); //far plane
}