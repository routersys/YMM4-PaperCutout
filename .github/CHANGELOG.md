# v1.0.0 - ペーパーカットアウト for YMM4

YukkuriMovieMaker4 向けのペーパーカットアウトエフェクトプラグインの初回リリースです。
Direct2D カスタムピクセルシェーダーで入力映像の輝度から紙の階層を手続き的に生成し、切り絵・紙工作風の立体感へ変換します。
参照画像や外部モデルを使わず、暗い部分ほど手前の紙として扱い、階層境界に影とハイライトを加えます。
紙目は座標に基づくノイズで生成し、同じ入力の同じ座標では常に同じ結果になります。
8 言語リソース構成の UI を備えます。

---

## 新機能

### 1. ピクセルシェーダー

`PaperCutout.hlsl` の `main` はフレームごとに入力映像の輝度を求め、3 から 8 段の紙階層へ分割します。隣接画素の階層差から切り口を検出し、光の方向に応じた影、ハイライト、紙目を合成します。追加テクスチャは使用しません。`source.a <= 1e-5` のときはソースをそのまま返します。

#### 輝度と階層化

入力色はプリマルチプライドの `rgb` を `rgb / max(alpha, 1e-5)` でストレート化し、Rec.709 係数 `(0.2126, 0.7152, 0.0722)` で輝度を求めます。

紙階層は `floor(saturate(1 − luma) × (layerCount − 1))` で生成します。明るい部分ほど奥、暗い部分ほど手前の紙として扱います。

| 項目 | 説明 |
|---|---|
| `layerCount` | 3〜8 に制限した紙階層数 |
| `maxLayer` | `layerCount − 1` |
| `luma` | ストレート化した入力色の Rec.709 輝度 |
| `rough` | 紙目に応じて階層境界へ加える微小なゆらぎ |
| `layerTone` | 階層の明るさ方向の値 |

#### 切り口の検出

`CutEdge` は中心画素の階層と上下左右 4 方向の階層を比較し、差の最大値を `maxLayer` で正規化します。階層差がある場所ほど切り口として扱われ、段差による明暗差に使われます。

| サンプル方向 | 用途 |
|---|---|
| 左右 | 水平方向の階層境界 |
| 上下 | 垂直方向の階層境界 |
| 中心 | 現在画素の紙階層 |

#### 影とハイライト

光の方向は `float2(cos(lightAngle), sin(lightAngle))` で求めます。影は光の反対方向、ハイライトは光の方向を参照します。

| 関数 | サンプル数 | 方向 | 説明 |
|---|---|---|---|
| `AccumulateShadow` | 8 | `-dir` | 手前の紙が低い階層へ落とす影 |
| `AccumulateHighlight` | 4 | `dir` | 切り口の明るい縁 |

影は `shadowMask × shadow × (0.36 + depth × 0.014)` で暗くし、ハイライトは `highlightMask × relief × 0.22` で加算します。切り口には `edge × relief × 0.16` のリム光も加えます。

#### 紙目

紙目は `Hash21`、`Noise2`、4 オクターブの `FractalNoise` で生成します。繊維方向のノイズと細かな斑点を混ぜ、紙のざらつきとして出力色へ反映します。

| 値 | 説明 |
|---|---|
| `fiber` | 繊維方向のゆらぎ |
| `speckle` | 座標セル単位の斑点 |
| `grainValue` | 紙色へ掛ける明暗ゆらぎ |

#### 合成

紙色と元色は `colorRetention` で補間します。`amount` が 0 のときは入力映像、1 のときは紙階層化した結果になります。

| 項目 | 式 |
|---|---|
| 紙色階調 | `paperColor × (0.62 + layerTone × 0.48)` |
| 元色階調 | `sourceRgb × (0.74 + layerTone × 0.36)` |
| 紙色と元色 | `lerp(paperTone, retainedColor, colorRetention)` |
| 出力色 | `lerp(sourceRgb, cutout, amount)` |

最終出力は入力アルファを保持し、`float4(straight × alpha, alpha)` をプリマルチプライドで返します。

---

### 2. カスタムシェーダーエフェクト

`PaperCutoutCustomEffect` は `[CustomEffect(1)]` の 1 入力エフェクトです。公開プロパティは `SetValue` を介して定数バッファーへ転送します。

| プロパティ | 型 | 範囲 |
|---|---|---|
| `InputLeft` | `float` | 入力矩形の左 |
| `InputTop` | `float` | 入力矩形の上 |
| `InputWidth` | `float` | 1 以上 |
| `InputHeight` | `float` | 1 以上 |
| `Amount` | `float` | 0〜1 |
| `Depth` | `float` | 0 以上 |
| `Shadow` | `float` | 0〜1 |
| `Relief` | `float` | 0〜1 |
| `Grain` | `float` | 0〜1 |
| `LightAngle` | `float` | ラジアン |
| `ColorRetention` | `float` | 0〜1 |
| `LayerCount` | `float` | 3〜8 |
| `PaperR` | `float` | 0〜1 |
| `PaperG` | `float` | 0〜1 |
| `PaperB` | `float` | 0〜1 |
| `PaperA` | `float` | 0〜1 |

`ConstantBuffer` のレイアウトは以下のとおりです。

| フィールド | 型 | 説明 |
|---|---|---|
| `InputLeft` | `float` | 入力矩形の左 |
| `InputTop` | `float` | 入力矩形の上 |
| `InputWidth` | `float` | 入力矩形の幅 |
| `InputHeight` | `float` | 入力矩形の高さ |
| `Amount` | `float` | 適用量 |
| `Depth` | `float` | 奥行き |
| `Shadow` | `float` | 影 |
| `Relief` | `float` | 段差 |
| `Grain` | `float` | 紙目 |
| `LightAngle` | `float` | 光の角度 |
| `ColorRetention` | `float` | 元色 |
| `LayerCount` | `float` | 階層数 |
| `PaperR` | `float` | 紙色 R |
| `PaperG` | `float` | 紙色 G |
| `PaperB` | `float` | 紙色 B |
| `PaperA` | `float` | 紙色 A |

`MapInputRectsToOutputRect` は入力 0 の矩形を出力矩形に設定し、入力矩形の位置とサイズを定数バッファーに書き込みます。`MapOutputRectToInputRects` は影とハイライトの参照に必要な分だけ入力矩形を拡張します。拡張量は `ceil(depth) + 4` で、上限は 4096px です。

シェーダーリソース: `pack://application:,,,/PaperCutout;component/Shaders/PaperCutout.cso`（ps_5_0、`ShaderResourceUri.Get` が生成）

---

### 3. エフェクト定義

`PaperCutoutEffect` は YMM4 の映像エフェクトとして宣言されます。

`[VideoEffect]` 属性は以下のパラメーターで宣言されます。

- 表示名：`Texts.PaperCutoutEffectName`（ローカライズキー）
- カテゴリー：`VideoEffectCategories.Decoration`
- 検索タグ：`TagPaper`・`TagCutout`・`TagLayer`・`TagCraft`・`paper cutout`・`cut paper`
- `IsAviUtlSupported = false` により AviUtl 向け EXO 出力は非対応
- `ResourceType = typeof(Texts)` でローカライズリソースを指定

`Label` プロパティは `Texts.PaperCutoutEffectName` を返します。

公開プロパティは以下のとおりです。

| プロパティ | 型 | デフォルト | 内部範囲 | アニメーション |
|---|---|---|---|---|
| `LayerCount` | `int` | 5 | 3〜8 | なし |
| `Depth` | `Animation` | 6 | 0〜128 | あり |
| `Shadow` | `Animation` | 45 | 0〜100 | あり |
| `Relief` | `Animation` | 55 | 0〜100 | あり |
| `Grain` | `Animation` | 18 | 0〜100 | あり |
| `LightAngle` | `Animation` | 135 | -360〜360 | あり |
| `ColorRetention` | `Animation` | 70 | 0〜100 | あり |
| `PaperColor` | `Color` | `#FFF6EEDA` | — | なし |
| `Amount` | `Animation` | 100 | 0〜100 | あり |

`GetAnimatables` は `Depth`・`Shadow`・`Relief`・`Grain`・`LightAngle`・`ColorRetention`・`Amount` を返します。

`CreateExoVideoFilters` は空のシーケンスを返します（EXO 非対応）。`CreateVideoEffect` は映像処理用のインスタンスを生成します。

---

### 4. フレームごとの更新

各フレームで YMM4 の `EffectDescription` からフレーム位置、アイテム長、FPS を取得し、アニメーション値を評価します。前フレームと値が異なる場合だけカスタムシェーダーへ値を転送します。

| パラメータ | 変換 |
|---|---|
| `LayerCount` | `Math.Clamp(value, 3, 8)` |
| `Depth` | 0 以上へ制限 |
| `Shadow` | `value / 100` を 0〜1 へ制限 |
| `Relief` | `value / 100` を 0〜1 へ制限 |
| `Grain` | `value / 100` を 0〜1 へ制限 |
| `LightAngle` | 度からラジアンへ変換 |
| `ColorRetention` | `value / 100` を 0〜1 へ制限 |
| `Amount` | `value / 100` を 0〜1 へ制限 |
| `PaperColor` | `R/G/B/A` を 0〜1 の float へ変換 |

入力は `SetInput(0, input, true)` でカスタムシェーダーへ接続します。エフェクトチェーンのクリア時は入力 0 を `null` に戻します。

---

### 5. ローカライズ

`Texts` クラスは `[AutoGenLocalizer]` 属性を持つ `partial` クラスとして宣言されます。
`YukkuriMovieMaker.Generator` のソースジェネレーターが `Texts.csv` を処理し、各ロケールのリソースファイルを自動生成します。

対応リソース：日本語（`ja-jp`）・英語（`en-us`）・中国語簡体字（`zh-cn`）・中国語繁体字（`zh-tw`）・韓国語（`ko-kr`）・スペイン語（`es-es`）・アラビア語（`ar-sa`）・インドネシア語（`id-id`）

ローカライズキーの一覧は以下のとおりです。

| キー | ja-jp |
|---|---|
| `PaperCutoutEffectName` | ペーパーカットアウト |
| `TagPaper` | 紙 |
| `TagCutout` | 切り絵 |
| `TagLayer` | レイヤー |
| `TagCraft` | クラフト |
| `LayerCountName` | 階層数 |
| `LayerCountDesc` | 輝度から自動生成する紙の階層数を指定します。 |
| `DepthName` | 奥行き |
| `DepthDesc` | 階層境界に落とす影の距離をpxで指定します。 |
| `ShadowName` | 影 |
| `ShadowDesc` | 紙の段差で発生する影の強さを調整します。 |
| `ReliefName` | 段差 |
| `ReliefDesc` | 切り口の明暗差と立体感を調整します。 |
| `GrainName` | 紙目 |
| `GrainDesc` | 紙の繊維状のざらつきを調整します。 |
| `LightAngleName` | 光の角度 |
| `LightAngleDesc` | 影とハイライトの向きを角度で指定します。 |
| `ColorRetentionName` | 元色 |
| `ColorRetentionDesc` | 元画像の色を残す割合を調整します。 |
| `PaperColorName` | 紙色 |
| `PaperColorDesc` | 紙として混ぜ込む基準色を指定します。 |
| `AmountName` | 適用量 |
| `AmountDesc` | 元画像とペーパーカットアウト結果の混合量を調整します。 |