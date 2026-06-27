using PaperCutout.Effect.Video.PaperCutout;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace PaperCutout;

[VideoEffect(nameof(Texts.PaperCutoutEffectName), [VideoEffectCategories.Decoration], [nameof(Texts.TagPaper), nameof(Texts.TagCutout), nameof(Texts.TagLayer), nameof(Texts.TagCraft), "paper cutout", "cut paper"], IsAviUtlSupported = false, ResourceType = typeof(Texts))]
public sealed class PaperCutoutEffect : VideoEffectBase
{
    public override string Label => Texts.PaperCutoutEffectName;

    [Display(GroupName = nameof(Texts.PaperCutoutEffectName), Name = nameof(Texts.LayerCountName), Description = nameof(Texts.LayerCountDesc), Order = 100, ResourceType = typeof(Texts))]
    [TextBoxSlider("F0", "", 3, 8)]
    [Range(3, 8)]
    [DefaultValue(5)]
    public int LayerCount { get => _layerCount; set => Set(ref _layerCount, value); }
    private int _layerCount = 5;

    [Display(GroupName = nameof(Texts.PaperCutoutEffectName), Name = nameof(Texts.DepthName), Description = nameof(Texts.DepthDesc), Order = 101, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "px", 0d, 24d)]
    public Animation Depth { get; } = new Animation(6, 0, 128);

    [Display(GroupName = nameof(Texts.PaperCutoutEffectName), Name = nameof(Texts.ShadowName), Description = nameof(Texts.ShadowDesc), Order = 102, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation Shadow { get; } = new Animation(45, 0, 100);

    [Display(GroupName = nameof(Texts.PaperCutoutEffectName), Name = nameof(Texts.ReliefName), Description = nameof(Texts.ReliefDesc), Order = 103, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation Relief { get; } = new Animation(55, 0, 100);

    [Display(GroupName = nameof(Texts.PaperCutoutEffectName), Name = nameof(Texts.GrainName), Description = nameof(Texts.GrainDesc), Order = 104, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation Grain { get; } = new Animation(18, 0, 100);

    [Display(GroupName = nameof(Texts.PaperCutoutEffectName), Name = nameof(Texts.LightAngleName), Description = nameof(Texts.LightAngleDesc), Order = 105, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "°", -180d, 180d)]
    public Animation LightAngle { get; } = new Animation(135, -360, 360);

    [Display(GroupName = nameof(Texts.PaperCutoutEffectName), Name = nameof(Texts.ColorRetentionName), Description = nameof(Texts.ColorRetentionDesc), Order = 106, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation ColorRetention { get; } = new Animation(70, 0, 100);

    [Display(GroupName = nameof(Texts.PaperCutoutEffectName), Name = nameof(Texts.PaperColorName), Description = nameof(Texts.PaperColorDesc), Order = 107, ResourceType = typeof(Texts))]
    [ColorPicker]
    public Color PaperColor { get => _paperColor; set => Set(ref _paperColor, value); }
    private Color _paperColor = Color.FromArgb(255, 246, 238, 218);

    [Display(GroupName = nameof(Texts.PaperCutoutEffectName), Name = nameof(Texts.AmountName), Description = nameof(Texts.AmountDesc), Order = 108, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation Amount { get; } = new Animation(100, 0, 100);

    public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription) => [];

    public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        => new PaperCutoutEffectProcessor(devices, this);

    protected override IEnumerable<IAnimatable> GetAnimatables()
        => [Depth, Shadow, Relief, Grain, LightAngle, ColorRetention, Amount];
}
