Texture2D InputTexture : register(t0);
SamplerState InputSampler : register(s0);

cbuffer Constants : register(b0)
{
    float inputLeft;
    float inputTop;
    float inputWidth;
    float inputHeight;
    float amount;
    float depth;
    float shadow;
    float relief;
    float grain;
    float lightAngle;
    float colorRetention;
    float layerCount;
    float paperR;
    float paperG;
    float paperB;
    float paperA;
};

static const float EPSILON = 1e-5f;

float Hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031f);
    p3 += dot(p3, p3.yzx + 33.33f);
    return frac((p3.x + p3.y) * p3.z);
}

float Noise2(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0f - 2.0f * f);
    float a = Hash21(i);
    float b = Hash21(i + float2(1.0f, 0.0f));
    float c = Hash21(i + float2(0.0f, 1.0f));
    float d = Hash21(i + float2(1.0f, 1.0f));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float FractalNoise(float2 p)
{
    float value = 0.0f;
    float amplitude = 0.5f;
    float norm = 0.0f;

    [unroll]
    for (int i = 0; i < 4; i++)
    {
        value += Noise2(p) * amplitude;
        norm += amplitude;
        p = p * 2.13f + 17.17f;
        amplitude *= 0.5f;
    }

    return value / max(norm, EPSILON);
}

float3 SampleStraight(float2 uv)
{
    float4 color = InputTexture.SampleLevel(InputSampler, uv, 0);
    return color.a > EPSILON ? saturate(color.rgb / color.a) : float3(0.0f, 0.0f, 0.0f);
}

float SampleAlpha(float2 uv)
{
    return saturate(InputTexture.SampleLevel(InputSampler, uv, 0).a);
}

float Luminance(float3 color)
{
    return dot(color, float3(0.2126f, 0.7152f, 0.0722f));
}

float ComputeLayer(float2 uv, float2 scene, float fallback)
{
    float alpha = SampleAlpha(uv);
    if (alpha <= EPSILON)
        return fallback;

    float rough = (FractalNoise(scene * 0.055f) - 0.5f) * grain * 0.18f;
    float luma = saturate(Luminance(SampleStraight(uv)) + rough);
    float count = clamp(round(layerCount), 3.0f, 8.0f);
    return floor(saturate(1.0f - luma) * (count - 1.0f));
}

float CutEdge(float layer, float2 uv, float2 uvStep, float2 scene)
{
    float l = ComputeLayer(uv + float2(-uvStep.x, 0.0f), scene + float2(-1.0f, 0.0f), layer);
    float r = ComputeLayer(uv + float2(uvStep.x, 0.0f), scene + float2(1.0f, 0.0f), layer);
    float u = ComputeLayer(uv + float2(0.0f, -uvStep.y), scene + float2(0.0f, -1.0f), layer);
    float d = ComputeLayer(uv + float2(0.0f, uvStep.y), scene + float2(0.0f, 1.0f), layer);
    float maxLayer = max(clamp(round(layerCount), 3.0f, 8.0f) - 1.0f, 1.0f);
    float e = max(max(abs(layer - l), abs(layer - r)), max(abs(layer - u), abs(layer - d)));
    return saturate(e / maxLayer);
}

float AccumulateShadow(float layer, float2 uv, float2 uvStep, float2 scene, float2 dir)
{
    float result = 0.0f;
    float radius = max(depth, 0.0f);
    float maxLayer = max(clamp(round(layerCount), 3.0f, 8.0f) - 1.0f, 1.0f);

    [unroll]
    for (int i = 1; i <= 8; i++)
    {
        float t = float(i) / 8.0f;
        float px = max(radius * t, 1.0f);
        float2 offsetUv = -dir * px * uvStep;
        float2 offsetScene = -dir * px;
        float sampled = ComputeLayer(uv + offsetUv, scene + offsetScene, layer);
        float overlap = saturate((layer - sampled) / maxLayer);
        result = max(result, overlap * (1.0f - t * 0.72f));
    }

    return result;
}

float AccumulateHighlight(float layer, float2 uv, float2 uvStep, float2 scene, float2 dir)
{
    float result = 0.0f;
    float radius = max(depth * 0.55f, 0.0f);
    float maxLayer = max(clamp(round(layerCount), 3.0f, 8.0f) - 1.0f, 1.0f);

    [unroll]
    for (int i = 1; i <= 4; i++)
    {
        float t = float(i) / 4.0f;
        float px = max(radius * t, 1.0f);
        float2 offsetUv = dir * px * uvStep;
        float2 offsetScene = dir * px;
        float sampled = ComputeLayer(uv + offsetUv, scene + offsetScene, layer);
        float exposed = saturate((sampled - layer) / maxLayer);
        result = max(result, exposed * (1.0f - t * 0.64f));
    }

    return result;
}

float4 main(float4 pos : SV_POSITION, float4 posScene : SCENE_POSITION, float4 uv0 : TEXCOORD0) : SV_TARGET
{
    float4 source = InputTexture.Sample(InputSampler, uv0.xy);
    float alpha = saturate(source.a);

    if (alpha <= EPSILON)
        return source;

    float2 scene = float2(posScene.x - inputLeft, posScene.y - inputTop);
    float2 uvStep = uv0.zw;
    float3 sourceStraight = saturate(source.rgb / max(alpha, EPSILON));
    float centerLuma = Luminance(sourceStraight);
    float count = clamp(round(layerCount), 3.0f, 8.0f);
    float maxLayer = max(count - 1.0f, 1.0f);
    float currentLayer = ComputeLayer(uv0.xy, scene, floor(saturate(1.0f - centerLuma) * maxLayer));
    float layerTone = 1.0f - currentLayer / maxLayer;
    float edge = CutEdge(currentLayer, uv0.xy, uvStep, scene);
    float2 dir = normalize(float2(cos(lightAngle), sin(lightAngle)));

    float shadowMask = AccumulateShadow(currentLayer, uv0.xy, uvStep, scene, dir);
    float highlightMask = AccumulateHighlight(currentLayer, uv0.xy, uvStep, scene, dir);
    float3 paperColor = saturate(float3(paperR, paperG, paperB));
    float paperAlpha = saturate(paperA);
    float3 paperTone = paperColor * (0.62f + layerTone * 0.48f);
    float3 retainedColor = sourceStraight * (0.74f + layerTone * 0.36f);
    float3 cutout = lerp(paperTone, retainedColor, saturate(colorRetention)) * lerp(1.0f, paperAlpha, 0.25f);

    float fiber = FractalNoise(scene * float2(0.42f, 0.09f) + float2(Noise2(scene * 0.017f), 0.0f));
    float speckle = Hash21(floor(scene * 0.85f));
    float grainValue = (fiber - 0.5f) * 0.18f + (speckle - 0.5f) * 0.06f;
    cutout *= 1.0f + grainValue * grain;

    float shade = 1.0f - shadowMask * shadow * (0.36f + depth * 0.014f);
    float shine = highlightMask * relief * 0.22f;
    float rim = edge * relief * 0.16f;
    cutout = cutout * saturate(shade) + shine + rim;
    cutout = saturate(cutout);

    float3 straight = lerp(sourceStraight, cutout, saturate(amount));
    return float4(straight * alpha, alpha);
}
