namespace PaperCutout;

internal static class ShaderResourceUri
{
    public static Uri Get(string shaderName) => new($"pack://application:,,,/PaperCutout;component/Shaders/{shaderName}.cso");
}
