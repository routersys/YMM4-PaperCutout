using System.Windows.Media;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Player.Video.Effects;

namespace PaperCutout;

internal sealed class PaperCutoutEffectProcessor(IGraphicsDevicesAndContext devices, PaperCutoutEffect item) : VideoEffectProcessorBase(devices)
{
    private readonly PaperCutoutEffect _item = item;
    private PaperCutoutCustomEffect? _effect;
    private bool _isFirst = true;
    private int _layerCount;
    private double _depth;
    private double _shadow;
    private double _relief;
    private double _grain;
    private double _lightAngle;
    private double _colorRetention;
    private double _amount;
    private Color _paperColor;

    public override DrawDescription Update(EffectDescription effectDescription)
    {
        if (IsPassThroughEffect || _effect is null)
            return effectDescription.DrawDescription;

        var frame = effectDescription.ItemPosition.Frame;
        var length = effectDescription.ItemDuration.Frame;
        var fps = effectDescription.FPS;

        var layerCount = Math.Clamp(_item.LayerCount, 3, 8);
        var depth = _item.Depth.GetValue(frame, length, fps);
        var shadow = _item.Shadow.GetValue(frame, length, fps) / 100d;
        var relief = _item.Relief.GetValue(frame, length, fps) / 100d;
        var grain = _item.Grain.GetValue(frame, length, fps) / 100d;
        var lightAngle = _item.LightAngle.GetValue(frame, length, fps) * Math.PI / 180d;
        var colorRetention = _item.ColorRetention.GetValue(frame, length, fps) / 100d;
        var amount = _item.Amount.GetValue(frame, length, fps) / 100d;
        var paperColor = _item.PaperColor;

        if (_isFirst || _layerCount != layerCount)
            _effect.LayerCount = layerCount;
        if (_isFirst || _depth != depth)
            _effect.Depth = (float)Math.Max(depth, 0d);
        if (_isFirst || _shadow != shadow)
            _effect.Shadow = (float)Math.Clamp(shadow, 0d, 1d);
        if (_isFirst || _relief != relief)
            _effect.Relief = (float)Math.Clamp(relief, 0d, 1d);
        if (_isFirst || _grain != grain)
            _effect.Grain = (float)Math.Clamp(grain, 0d, 1d);
        if (_isFirst || _lightAngle != lightAngle)
            _effect.LightAngle = (float)lightAngle;
        if (_isFirst || _colorRetention != colorRetention)
            _effect.ColorRetention = (float)Math.Clamp(colorRetention, 0d, 1d);
        if (_isFirst || _amount != amount)
            _effect.Amount = (float)Math.Clamp(amount, 0d, 1d);
        if (_isFirst || _paperColor != paperColor)
        {
            _effect.PaperR = paperColor.R / 255f;
            _effect.PaperG = paperColor.G / 255f;
            _effect.PaperB = paperColor.B / 255f;
            _effect.PaperA = paperColor.A / 255f;
        }

        _isFirst = false;
        _layerCount = layerCount;
        _depth = depth;
        _shadow = shadow;
        _relief = relief;
        _grain = grain;
        _lightAngle = lightAngle;
        _colorRetention = colorRetention;
        _amount = amount;
        _paperColor = paperColor;

        return effectDescription.DrawDescription;
    }

    protected override ID2D1Image? CreateEffect(IGraphicsDevicesAndContext devices)
    {
        var effect = new PaperCutoutCustomEffect(devices);
        if (!effect.IsEnabled)
        {
            effect.Dispose();
            return null;
        }

        _effect = effect;
        disposer.Collect(_effect);

        var output = _effect.Output;
        disposer.Collect(output);
        return output;
    }

    protected override void setInput(ID2D1Image? input)
    {
        _effect?.SetInput(0, input, true);
    }

    protected override void ClearEffectChain()
    {
        _effect?.SetInput(0, null, true);
    }
}
