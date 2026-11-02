using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Nekkoya.VRCDataChecker
{
    public static class TextureRules
    {
        private static bool Has(string value, params string[] words) => words.Any(w => value.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);
        public static bool IsNormalName(string path) => Has(Path.GetFileNameWithoutExtension(path), "normal", "ノーマル");
        public static bool IsMaxSizeOverspecified(int sourceWidth, int sourceHeight, int maxSize) => sourceWidth > 0 && sourceHeight > 0 && maxSize > Math.Max(sourceWidth, sourceHeight);
        public static bool MatchesInitialSettings(TextureImporter importer)
        {
            var platform = importer.GetDefaultPlatformTextureSettings();
            // Compare the visible settings in the supplied screenshot, not edit history.
            bool matches = importer.textureType == TextureImporterType.Default && importer.textureShape == TextureImporterShape.Texture2D &&
                importer.sRGBTexture && importer.alphaSource == TextureImporterAlphaSource.FromInput && !importer.alphaIsTransparency &&
                importer.npotScale == TextureImporterNPOTScale.ToNearest && !importer.isReadable && !importer.vtOnly &&
                importer.mipmapEnabled && !importer.ignoreMipmapLimit && string.IsNullOrEmpty(importer.mipmapLimitGroupName) &&
                !importer.streamingMipmaps && importer.mipmapFilter == TextureImporterMipFilter.BoxFilter &&
                !importer.mipMapsPreserveCoverage && !importer.borderMipmap && !importer.fadeout && !importer.ignorePngGamma &&
                importer.swizzleR == TextureImporterSwizzle.R && importer.swizzleG == TextureImporterSwizzle.G &&
                importer.swizzleB == TextureImporterSwizzle.B && importer.swizzleA == TextureImporterSwizzle.A &&
                importer.wrapModeU == TextureWrapMode.Repeat && importer.wrapModeV == TextureWrapMode.Repeat && importer.wrapModeW == TextureWrapMode.Repeat &&
                importer.filterMode == FilterMode.Bilinear && importer.anisoLevel == 1 &&
                platform.maxTextureSize == 2048 && platform.resizeAlgorithm == TextureResizeAlgorithm.Mitchell &&
                platform.format == TextureImporterFormat.Automatic && platform.textureCompression == TextureImporterCompression.Compressed && !platform.crunchedCompression;
            if (!matches) return false;
            // Any explicit platform override counts as a changed import configuration.
            using (var serialized = new SerializedObject(importer))
            {
                var platforms = serialized.FindProperty("m_PlatformSettings");
                if (platforms == null || !platforms.isArray) return false;
                for (int i = 0; i < platforms.arraySize; i++)
                {
                    var entry = platforms.GetArrayElementAtIndex(i);
                    var overridden = entry.FindPropertyRelative("m_Overridden");
                    if (overridden == null || overridden.boolValue) return false;
                }
            }
            return true;
        }
        public static void Evaluate(CheckContext context, string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            var texture = context.Load(path).OfType<Texture>().FirstOrDefault();
            if (texture == null) return;
            context.textureMemoryBytes += Profiler.GetRuntimeMemorySizeLong(texture);
            var uses = context.textureUses.TryGetValue(path, out var list) ? list : new List<TextureUse>();
            string properties = string.Join(", ", uses.Select(u => u.property).Distinct());
            bool normal = IsNormalName(path);
            bool data = Has(properties, "mask", "metallic", "smoothness", "occlusion") || uses.Any(u => u.property.Equals("_AO", StringComparison.OrdinalIgnoreCase));
            bool rampProperty = Has(properties, "ramp", "lut", "lookup", "data");
            bool rampName = Has(texture.name, "ramp", "lut", "lookup");
            bool color = Has(properties, "maintex", "basemap", "basecolor", "albedo", "emission");
            bool sprite = importer.textureType == TextureImporterType.Sprite || importer.textureType == TextureImporterType.GUI;
            bool used3D = uses.Any(u => u.usedByRenderer);
            if (!importer.mipmapEnabled && used3D && !sprite && !rampProperty)
                context.Add("TEX_MIPMAP_001", rampName ? CheckSeverity.Info : CheckSeverity.Warning, CheckCategory.Texture, path,
                    "3D RendererのMaterialで使用されていますがMipmapが無効です。" + (rampName ? "名前からLUT等の可能性もあります。" : ""), "通常の3D表示用TextureならMipmapを有効にしてください。用途を確認して判断してください。");
            if (!importer.mipmapEnabled && importer.textureType == TextureImporterType.NormalMap)
                context.Add("TEX_MIPMAP_002", CheckSeverity.Warning, CheckCategory.Texture, path, "Normal MapのMipmapが無効です。", "3D表示用ならMipmapを有効にしてください。");
            if (importer.mipmapEnabled && importer.filterMode == FilterMode.Point)
                context.Add("TEX_MIPMAP_003", CheckSeverity.Info, CheckCategory.Texture, path, "Mipmapが有効でFilter ModeがPointです。");
            if (importer.isReadable)
                context.Add("TEX_READABLE_001", CheckSeverity.Warning, CheckCategory.Texture, path, "TextureのRead/Writeが有効です。表示用とは別に、スクリプトから読み書きするためのメモリを使います。",
                    "【OFFでよい例】通常のMaterialに貼って表示する画像、マスク、MatCap、ノーマルマップ。\n【ONが必要な例】スクリプトがGetPixels／SetPixelsなどで画像のピクセルを読み書きする、画像編集ツールがRead/Writeを要求する場合。\n特別なツールで使わず表示だけなら、TextureのInspector → Advanced → Read/WriteをOFFにしてApply。スクリプトからの利用は自動判定できません。");
            int width = texture.width, height = texture.height;
            string sizeSource = "元画像サイズ";
            try { importer.GetSourceTextureWidthAndHeight(out width, out height); }
            catch { width = texture.width; height = texture.height; sizeSource = "import後サイズ"; }
            if (width <= 0 || height <= 0) { width = texture.width; height = texture.height; sizeSource = "import後サイズ"; }
            int importedSize = Math.Max(texture.width, texture.height);
            var limits = context.settings.thresholds;
            int limit = sprite ? limits.iconTextureWarningSize : rampProperty ? limits.rampTextureWarningSize : Has(properties, "matcap") ? limits.matcapTextureWarningSize : data ? limits.maskTextureWarningSize : limits.generalTextureWarningSize;
            if (importedSize >= limit)
                context.Add("TEX_SIZE_001", CheckSeverity.Warning, CheckCategory.Texture, path, (importedSize >= 8192 ? "大容量Texture: " : "") + "import後サイズ " + texture.width + "×" + texture.height + "、用途別警告値 " + limit + "以上。",
                    "見た目を確認しながら解像度／Max Sizeを調整してください。これは本ツールの推奨値です。");
            if (!Mathf.IsPowerOfTwo(width) || !Mathf.IsPowerOfTwo(height)) context.Add("TEX_NPOT_001", CheckSeverity.Info, CheckCategory.Texture, path, sizeSource + "が2の累乗ではありません: " + width + "×" + height);
            if (sizeSource == "元画像サイズ")
            {
                CheckMaxSize(context, path, width, height, importer.maxTextureSize, "Default");
                if ((context.platforms & TargetPlatformMode.PC) != 0)
                {
                    var pc = importer.GetPlatformTextureSettings("Standalone");
                    if (pc.overridden) CheckMaxSize(context, path, width, height, pc.maxTextureSize, "PC Override");
                }
                if (context.Mobile)
                {
                    var mobile = importer.GetPlatformTextureSettings("Android");
                    if (mobile.overridden) CheckMaxSize(context, path, width, height, mobile.maxTextureSize, "Android Override");
                }
            }
            if (MatchesInitialSettings(importer))
                context.Add("TEX_DEFAULT_001", CheckSeverity.Warning, CheckCategory.Texture, path,
                    "添付画像の初期設定と一致しています（Default / sRGB ON / Mipmap ON / Max Size 2048 / Normal Quality）。",
                    "用途に合わせた設定確認が済んでいるか確認してください。設定が一致することを検出しており、編集履歴の有無は判定できません。");
            if (color && !importer.sRGBTexture && importer.textureType != TextureImporterType.NormalMap)
                context.Add("TEX_SRGB_001", CheckSeverity.Warning, CheckCategory.Texture, path, "プロパティ名からColor用途と推定しましたがsRGBがOFFです: " + properties, "Shader独自仕様を確認して色空間を設定してください。");
            if (data && importer.sRGBTexture)
                context.Add("TEX_SRGB_002", CheckSeverity.Warning, CheckCategory.Texture, path, "色ではなく、光沢・金属感・マスクの強さを表す画像と推定しましたがsRGBがONです: " + properties,
                    "こうした数値用の画像は通常sRGBをOFFにします。Shaderの仕様を確認してください。");
            if (normal && importer.textureType != TextureImporterType.NormalMap)
                context.Add("TEX_NORMAL_001", CheckSeverity.Warning, CheckCategory.Texture, path, "画像名にNormal／ノーマルを含みますが、Texture TypeがNormal Mapではありません。", "凹凸を表すノーマルマップならTexture TypeをNormal Mapにしてください。");
            if (Math.Max(texture.width, texture.height) >= 2048 && importer.textureCompression == TextureImporterCompression.Uncompressed)
                context.Add("TEX_COMPRESS_001", CheckSeverity.Warning, CheckCategory.Texture, path, "大きなTextureの基本設定がUncompressedです。", "プラットフォームOverrideも確認し、品質に適した圧縮を検討してください。Crunchは実行時VRAM削減を意味しません。");
            if (context.Mobile)
            {
                var android = importer.GetPlatformTextureSettings("Android");
                if (!android.overridden) context.Add("TEX_MOBILE_001", CheckSeverity.Info, CheckCategory.Texture, path, "Android Overrideがありません。", "Mobile向けのサイズと形式を確認してください。推奨候補: " + context.settings.recommendedAndroidFormats);
                else if (!context.settings.recommendedAndroidFormats.Split(',').Any(p => p.Trim().Length > 0 && android.format.ToString().IndexOf(p.Trim(), StringComparison.OrdinalIgnoreCase) >= 0))
                    context.Add("TEX_MOBILE_002", CheckSeverity.Warning, CheckCategory.Texture, path, "Android形式が設定された推奨候補と異なります: " + android.format, "用途に応じて確認してください。推奨候補: " + context.settings.recommendedAndroidFormats);
            }
            foreach (var result in context.results.Where(r => r.assetPath == path && r.category == CheckCategory.Texture))
                result.message += uses.Count == 0 ? "" : "\nMaterial用途: " + string.Join("; ", uses.Select(u => u.materialPath + " / " + u.property));
        }
        private static void CheckMaxSize(CheckContext context, string path, int width, int height, int maxSize, string platform)
        {
            if (IsMaxSizeOverspecified(width, height, maxSize))
                context.Add("TEX_MAXSIZE_001", CheckSeverity.Warning, CheckCategory.Texture, path,
                    platform + "のMax Size " + maxSize + "が元画像 " + width + "×" + height + "を上回っています。",
                    "Max Sizeを元画像以下に設定してください。この設定だけで画像が拡大されるわけではありません。");
        }
    }
}
