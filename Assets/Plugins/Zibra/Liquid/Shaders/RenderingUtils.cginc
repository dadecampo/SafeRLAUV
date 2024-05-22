#define FAR_DISTANCE 1e5
#define RENDERING_EPS 1.0e-3
#define DIVISION_EPS 1.0e-9

Texture3D<float4> GridNormals;
SamplerState samplerGridNormals;
float4 FetchGridNormals(float3 uvw)
{
    return GridNormals.SampleLevel(samplerGridNormals, uvw, 0);
}

float FetchGridDensity(float3 uvw)
{
    // Unity 2020.3 seems to have a bug and doesn't bind more than 8 textures
    // Also, same bugs happen in builds in newer versions too
    // To work around that we need to decrease number of used textures to 8
    // So we save 1 slot by re-using bound normals texture
    // So we skipping density texture
    // Technically not same data, but works for current usecase
    return GridNormals.SampleLevel(samplerGridNormals, uvw, 0).w;
}

float3 RayDepths;
float3 Depths;
float3 Material;

float3 Material1Color;
float3 Material2Color;
float3 Material3Color;
float3 Material1Emission;
float3 Material2Emission;
float3 Material3Emission;
float3 MatAbsorption;
float3 MatScattering;
float3 MatMetalness;
float3 MatRoughness;

float FresnelStrength;

float Sum(float4 x)
{
    return x.x + x.y + x.z + x.w;
}

float Sum(float3 x)
{
    return x.x + x.y + x.z;
}

struct LightPath
{
    float3 depth;
    float3 material;
};

float3 WorldToUVW(float3 p)
{
    return (p - (ContainerPosition - ContainerScale * 0.5)) / ContainerScale + 0.5/GridSize;
}

float3 GetNodeF(float3 p)
{
    return GridSize * WorldToUVW(p);
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

float3 ComputeScattering(float depth)
{
    if (depth >= FAR_DISTANCE) return 1.0;
    return exp(min(-depth * ScatteringAmount, 0.0));
}

float3 FresnelTermLiquid(float3 F0, float cosA)
{
    float t = pow(1 - cosA, 5.0 * (2.0 - FresnelStrength));
    return F0 + (1 - F0) * t;
}

// Beer-Lambert law
float3 ComputeAbsorption(float depth)
{
    return exp(min(-(1.0 - RefractionColor.xyz) * depth * AbsorptionAmount, 0.0));
}

float4 MaterialWeights(float3 mat)
{
    float matsum = mat.x + mat.y + mat.z + 1e-6;
    float weight = clamp(matsum, 0.0, 1.0);
    return float4(mat * weight / matsum, 1.0 - weight);
}
//opticalDensity is the path length
//incomingLight is the light in the direction of the ray
//illumination is the light coming from light sources at the point of sampling(currently assumed constant)
//should be dependent on the shadowmap, otherwise "glow"
float3 IntegrateAbsorptionScattering(float opticalDensity, float3 incomingLight, float3 illumination)
{
    float4 weights = MaterialWeights(Material);

    float3 materialColor = Material1Color * weights.x + Material2Color      * weights.y +
                           Material3Color * weights.z + RefractionColor.xyz * weights.w;

    // emission coefficient
    float sigmaI = 0.0;
    // scattering coefficient
    float sigmaS = Sum(float4(MatScattering, ScatteringAmount) * weights);
    // absorption coefficient
    float sigmaA = Sum(float4(MatAbsorption, AbsorptionAmount) * weights);

    //average side length
    float SimulationScale = (1.0f / 3.0f) * (ContainerScale.x + ContainerScale.y + ContainerScale.z);
    float scaledOpticalDensity = opticalDensity / SimulationScale;

    // extinction (= outscatter + absorption) coefficient
    float3 sigmaE = max(DIVISION_EPS, sigmaS + (1.0 - materialColor) * sigmaA);
    // lighting (= inscatter + emission) coefficient
    float3 sigmaL = max(DIVISION_EPS, sigmaI + sigmaS);

    float3  extinction = exp(- sigmaE * scaledOpticalDensity);

    // See slide 28 at http://www.frostbite.com/2015/08/physically-based-unified-volumetric-rendering-in-frostbite/
    const float phaseFunction = 1.0;
    float3 emissColor = clamp(materialColor, 0.0, 1.0) * illumination; // environment light
    float3 S     =  emissColor * sigmaL * phaseFunction;     // incoming light
    float3 Sint  = S * (1.0 - extinction) / sigmaE;         // integrate along the current step segment

    return Sint + incomingLight * extinction;
}

float SampleDensity(float3 pos)
{
    return FetchGridDensity(WorldToUVW(pos));
}

float3 SampleNormals(float3 pos)
{
    return normalize(GridNormals.SampleLevel(samplerGridNormals, WorldToUVW(pos), 0).xyz + RENDERING_EPS);
}

float3 DecodeDirection( uint data )
{
    float2 v = float2(f16tof32(data >> 16), f16tof32(data)); v = v * 2.0 - 1.0;

    float3 nor = float3(v, 1.0 - abs(v.x) - abs(v.y));  // Rune Stubbe's version,
    float t = max(-nor.z,0.0);                          // much faster than original
    nor.x += (nor.x>0.0)?-t:t;                          // implementation of this
    nor.y += (nor.y>0.0)?-t:t;                          // technique

    return normalize( nor );
}

uint4 getQuad(uint quadID)
{
    uint gridCount = int(GridSize.x) * int(GridSize.y) * int(GridSize.z);
    uint axis = quadID / gridCount;
    uint3 voxel = getNode(quadID % gridCount);
    return uint4(voxel, axis);
}

uint3 getVoxel(uint4 quad, uint indexID)
{
    uint axis = quad.w;
    uint3 Ydir = uint3(((axis + 1) % 3)==0, ((axis + 1) % 3)==1, ((axis + 1) % 3)==2);
    uint3 Zdir = uint3(((axis + 2) % 3)==0, ((axis + 2) % 3)==1, ((axis + 2) % 3)==2);
    return quad.xyz + ((indexID & 1) > 0) * Ydir + ((indexID & 2) > 0) * Zdir;
}

float TraceRay(inout float3 pos, float3 ray, float depth)
{
    if (depth == 0.0) depth = FAR_DISTANCE;
    pos += ray * depth;
    return depth;
}

float4 AirLiquidBounce(inout float3 pos, float3 ray, float3 normal, float3 light, float2 uv)
{
    //fix artifacts with air ray depth
    if (RayDepths[1] < RENDERING_EPS) RayDepths[1] = FAR_DISTANCE;
    float AirDepth = TraceRay(pos, ray, RayDepths[1]);
    normal = SampleNormals(pos);   

    if (all(normal == 0)) AirDepth = FAR_DISTANCE;

    if (AirDepth >= FAR_DISTANCE) 
    {
        return float4(RefractSample(pos, ray, uv), 0);
    }
    else
    {
        float3 color = 0.0;
        normal = normalize(normal);

        float NV = abs(dot(normal, -ray)); 
        float fresnel = FresnelTermLiquid(Metalness, NV);
                
        float3 RefractRay = refract(ray, normal, 1.0 / LiquidIOR);
        float3 ReflectRay = reflect(ray, normal);
        float3 ReflectedColor = ReflectSample(pos, ReflectRay, Roughness);
        color += ReflectionColor.xyz * fresnel * ReflectedColor / Average(ReflectionColor.xyz);

        float LiquidDepth = TraceRay(pos, RefractRay, RayDepths[2]);

        float3 RefractNormal = -normalize(SampleNormals(pos));
        float3 SecondRefractRay = refract(RefractRay, RefractNormal, LiquidIOR);
        float3 opacity = ComputeAbsorption(LiquidDepth);
        float3 RefractColor;
        float opticalDensity = LiquidDepth;

        if (length(SecondRefractRay) > 0.5)
        {
            RefractColor = RefractSample(pos, SecondRefractRay, uv);
        }
        else //full internal reflection
        {
            float3 SecondReflectRay = reflect(RefractRay, RefractNormal);
            RefractColor = ReflectSample(pos, SecondReflectRay);
        }
    
        color += (1.0 - fresnel) * IntegrateAbsorptionScattering(opticalDensity, RefractColor, light);

        return float4(color, LiquidDepth);
    }
}

float3 RefractionRay(float3 pos, float3 cameraRay, float3 surfaceNormal, float2 uv, bool isUnderwater)
{
    float3 RefractPosition = pos;
    float opticalDensity = 0.0;
    float3 ray = cameraRay;
    float3 normal = surfaceNormal;

    if(!isUnderwater)
    {
        ray = refract(cameraRay, surfaceNormal, 1.0 / LiquidIOR);
        float LiquidDepth = TraceRay(RefractPosition, ray, RayDepths[0]);

        #ifdef STORE_DEPTH
            RayDepths.x = LiquidDepth; //store the depths in vertices
        #endif
        normal = -normalize(SampleNormals(RefractPosition));

        float3 RefractScreenPos = PositionToScreen(RefractPosition);
        #ifdef FLIP_NATIVE_TEXTURES
        RefractScreenPos.y = 1 - RefractScreenPos.y;
        #endif
        float4 CorrectedScenePosition = GetDepthAndPos(RefractScreenPos.xy);
        float CorrectedLiquidDepth = min(distance(CorrectedScenePosition.xyz, pos), LiquidDepth);
        opticalDensity += CorrectedLiquidDepth;
    }

    float3 RefractColor = 0.0;
    float3 RefractRay = refract(ray, normal, LiquidIOR);

    #ifdef HDRP
        float3 lightColor = LightColor;
        float3 lightDirWorld = LightDirection;
    #else
        float3 lightColor = _LightColor0;
        float3 lightDirWorld = normalize(_WorldSpaceLightPos0.xyz);
    #endif

    if(length(RefractRay) > 0.5)
    {
        float3 SecondPosition = RefractPosition;
        float4 SecondPath =
            AirLiquidBounce(SecondPosition, RefractRay, -normal, lightColor, uv);
        RefractColor = SecondPath.xyz;
    }
    else //full internal reflection
    {
        float3 SecondReflectRay = reflect(ray, normal);
        RefractColor = ReflectSample(pos, SecondReflectRay);
    }

    if (isUnderwater) return RefractColor;

    return IntegrateAbsorptionScattering(opticalDensity, RefractColor, lightColor);
}