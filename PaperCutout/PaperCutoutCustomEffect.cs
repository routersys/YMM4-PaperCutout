using System.Runtime.InteropServices;
using Vortice;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace PaperCutout;

internal sealed class PaperCutoutCustomEffect(IGraphicsDevicesAndContext devices) : D2D1CustomShaderEffectBase(Create<EffectImpl>(devices))
{
    public float InputLeft { set => SetValue((int)EffectImpl.Properties.InputLeft, value); }
    public float InputTop { set => SetValue((int)EffectImpl.Properties.InputTop, value); }
    public float InputWidth { set => SetValue((int)EffectImpl.Properties.InputWidth, value); }
    public float InputHeight { set => SetValue((int)EffectImpl.Properties.InputHeight, value); }
    public float Amount { set => SetValue((int)EffectImpl.Properties.Amount, value); }
    public float Depth { set => SetValue((int)EffectImpl.Properties.Depth, value); }
    public float Shadow { set => SetValue((int)EffectImpl.Properties.Shadow, value); }
    public float Relief { set => SetValue((int)EffectImpl.Properties.Relief, value); }
    public float Grain { set => SetValue((int)EffectImpl.Properties.Grain, value); }
    public float LightAngle { set => SetValue((int)EffectImpl.Properties.LightAngle, value); }
    public float ColorRetention { set => SetValue((int)EffectImpl.Properties.ColorRetention, value); }
    public float LayerCount { set => SetValue((int)EffectImpl.Properties.LayerCount, value); }
    public float PaperR { set => SetValue((int)EffectImpl.Properties.PaperR, value); }
    public float PaperG { set => SetValue((int)EffectImpl.Properties.PaperG, value); }
    public float PaperB { set => SetValue((int)EffectImpl.Properties.PaperB, value); }
    public float PaperA { set => SetValue((int)EffectImpl.Properties.PaperA, value); }

    [CustomEffect(1)]
    private sealed class EffectImpl : D2D1CustomShaderEffectImplBase<EffectImpl>
    {
        private ConstantBuffer _cb;

        [CustomEffectProperty(PropertyType.Float, (int)Properties.InputLeft)]
        public float InputLeft { get => _cb.InputLeft; set { _cb.InputLeft = value; UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.InputTop)]
        public float InputTop { get => _cb.InputTop; set { _cb.InputTop = value; UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.InputWidth)]
        public float InputWidth { get => _cb.InputWidth; set { _cb.InputWidth = Math.Max(value, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.InputHeight)]
        public float InputHeight { get => _cb.InputHeight; set { _cb.InputHeight = Math.Max(value, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Amount)]
        public float Amount { get => _cb.Amount; set { _cb.Amount = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Depth)]
        public float Depth { get => _cb.Depth; set { _cb.Depth = Math.Max(value, 0f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Shadow)]
        public float Shadow { get => _cb.Shadow; set { _cb.Shadow = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Relief)]
        public float Relief { get => _cb.Relief; set { _cb.Relief = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Grain)]
        public float Grain { get => _cb.Grain; set { _cb.Grain = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.LightAngle)]
        public float LightAngle { get => _cb.LightAngle; set { _cb.LightAngle = value; UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.ColorRetention)]
        public float ColorRetention { get => _cb.ColorRetention; set { _cb.ColorRetention = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.LayerCount)]
        public float LayerCount { get => _cb.LayerCount; set { _cb.LayerCount = Math.Clamp(value, 3f, 8f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.PaperR)]
        public float PaperR { get => _cb.PaperR; set { _cb.PaperR = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.PaperG)]
        public float PaperG { get => _cb.PaperG; set { _cb.PaperG = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.PaperB)]
        public float PaperB { get => _cb.PaperB; set { _cb.PaperB = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.PaperA)]
        public float PaperA { get => _cb.PaperA; set { _cb.PaperA = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

        public EffectImpl() : base(ShaderResourceUri.Get("PaperCutout"))
        {
            _cb.InputWidth = 1f;
            _cb.InputHeight = 1f;
            _cb.Amount = 1f;
            _cb.Depth = 6f;
            _cb.Shadow = 0.45f;
            _cb.Relief = 0.55f;
            _cb.Grain = 0.18f;
            _cb.LightAngle = 2.3561945f;
            _cb.ColorRetention = 0.7f;
            _cb.LayerCount = 5f;
            _cb.PaperR = 246f / 255f;
            _cb.PaperG = 238f / 255f;
            _cb.PaperB = 218f / 255f;
            _cb.PaperA = 1f;
        }

        protected override void UpdateConstants()
        {
            drawInformation?.SetPixelShaderConstantBuffer(_cb);
        }

        public override void MapInputRectsToOutputRect(RawRect[] inputRects, RawRect[] inputOpaqueSubRects, out RawRect outputRect, out RawRect outputOpaqueSubRect)
        {
            inputRect = ClampInputRect(inputRects[0]);
            outputRect = inputRect;
            outputOpaqueSubRect = inputOpaqueSubRects.Length > 0 ? inputOpaqueSubRects[0] : default;

            _cb.InputLeft = inputRect.Left;
            _cb.InputTop = inputRect.Top;
            _cb.InputWidth = Math.Max(inputRect.Right - inputRect.Left, 1);
            _cb.InputHeight = Math.Max(inputRect.Bottom - inputRect.Top, 1);
            UpdateConstants();
        }

        public override void MapOutputRectToInputRects(RawRect outputRect, RawRect[] inputRects)
        {
            var padding = Math.Min((int)Math.Ceiling(_cb.Depth) + 4, 4096);
            inputRects[0] = new RawRect(
                outputRect.Left - padding,
                outputRect.Top - padding,
                outputRect.Right + padding,
                outputRect.Bottom + padding);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ConstantBuffer
        {
            public float InputLeft;
            public float InputTop;
            public float InputWidth;
            public float InputHeight;
            public float Amount;
            public float Depth;
            public float Shadow;
            public float Relief;
            public float Grain;
            public float LightAngle;
            public float ColorRetention;
            public float LayerCount;
            public float PaperR;
            public float PaperG;
            public float PaperB;
            public float PaperA;
        }

        public enum Properties : int
        {
            InputLeft = 0,
            InputTop = 1,
            InputWidth = 2,
            InputHeight = 3,
            Amount = 4,
            Depth = 5,
            Shadow = 6,
            Relief = 7,
            Grain = 8,
            LightAngle = 9,
            ColorRetention = 10,
            LayerCount = 11,
            PaperR = 12,
            PaperG = 13,
            PaperB = 14,
            PaperA = 15,
        }
    }
}
