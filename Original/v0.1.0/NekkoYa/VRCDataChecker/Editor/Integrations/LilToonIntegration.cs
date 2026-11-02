using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nekkoya.VRCDataChecker
{
    // Read-only interpretation of lilToon's feature switches. No optional assembly reference.
    // Verified against lilToon 2.3.4 lilMaterialUtils.RemoveUnusedTextureOnly:
    // https://github.com/lilxyzw/lilToon/blob/master/Assets/lilToon/Editor/lilMaterialUtils.cs
    public static class LilToonIntegration
    {
        private static readonly Dictionary<string, string> FeatureSwitches = new Dictionary<string, string>
        {
            { "_MainGradationTex", "_MainGradationStrength" },
            { "_Main2ndTex", "_UseMain2ndTex" }, { "_Main2ndBlendMask", "_UseMain2ndTex" },
            { "_Main2ndDissolveMask", "_UseMain2ndTex" }, { "_Main2ndDissolveNoiseMask", "_UseMain2ndTex" },
            { "_Main3rdTex", "_UseMain3rdTex" }, { "_Main3rdBlendMask", "_UseMain3rdTex" },
            { "_Main3rdDissolveMask", "_UseMain3rdTex" }, { "_Main3rdDissolveNoiseMask", "_UseMain3rdTex" },
            { "_ShadowBlurMask", "_UseShadow" }, { "_ShadowBorderMask", "_UseShadow" },
            { "_ShadowStrengthMask", "_UseShadow" }, { "_ShadowColorTex", "_UseShadow" },
            { "_Shadow2ndColorTex", "_UseShadow" }, { "_Shadow3rdColorTex", "_UseShadow" },
            { "_RimShadeMask", "_UseRimShade" },
            { "_EmissionMap", "_UseEmission" }, { "_EmissionBlendMask", "_UseEmission" }, { "_EmissionGradTex", "_UseEmission" },
            { "_Emission2ndMap", "_UseEmission2nd" }, { "_Emission2ndBlendMask", "_UseEmission2nd" }, { "_Emission2ndGradTex", "_UseEmission2nd" },
            { "_BumpMap", "_UseBumpMap" }, { "_Bump2ndMap", "_UseBump2ndMap" }, { "_Bump2ndScaleMask", "_UseBump2ndMap" },
            { "_AnisotropyTangentMap", "_UseAnisotropy" }, { "_AnisotropyScaleMask", "_UseAnisotropy" }, { "_AnisotropyShiftNoiseMask", "_UseAnisotropy" },
            { "_SmoothnessTex", "_UseReflection" }, { "_MetallicGlossMap", "_UseReflection" }, { "_ReflectionColorTex", "_UseReflection" }, { "_ReflectionCubeTex", "_UseReflection" },
            { "_MatCapTex", "_UseMatCap" }, { "_MatCapBlendMask", "_UseMatCap" }, { "_MatCapBumpMap", "_UseMatCap" },
            { "_MatCap2ndTex", "_UseMatCap2nd" }, { "_MatCap2ndBlendMask", "_UseMatCap2nd" }, { "_MatCap2ndBumpMap", "_UseMatCap2nd" },
            { "_RimColorTex", "_UseRim" }, { "_GlitterColorTex", "_UseGlitter" }, { "_ParallaxMap", "_UseParallax" },
            { "_AudioLinkMask", "_UseAudioLink" }, { "_AudioLinkLocalMap", "_UseAudioLink" }
        };
        public static bool IsLilToon(Material material) => material != null && material.shader != null &&
            (material.shader.name.IndexOf("lilToon", StringComparison.OrdinalIgnoreCase) >= 0 || material.HasProperty("_lilToonVersion"));

        public static string InactiveReason(Material material, string property)
        {
            if (!IsLilToon(material) || string.IsNullOrEmpty(property)) return null;
            if (!material.HasProperty(property)) return "現在のShaderにない、以前の設定です。";
            // Unknown switches remain active: future/custom shader variants must not hide real errors.
            if (FeatureSwitches.TryGetValue(property, out var toggle) && material.HasProperty(toggle) && material.GetFloat(toggle) == 0f)
                return "対応する機能がOFFです（" + toggle + "）。";
            return null;
        }

        public static string TexturePropertyName(SerializedObject serialized, string propertyPath)
        {
            const string prefix = "m_SavedProperties.m_TexEnvs.Array.data[";
            if (!propertyPath.StartsWith(prefix, StringComparison.Ordinal) || !propertyPath.EndsWith(".second.m_Texture", StringComparison.Ordinal)) return null;
            int closing = propertyPath.IndexOf(']', prefix.Length);
            return closing < 0 ? null : serialized.FindProperty(propertyPath.Substring(0, closing + 1) + ".first")?.stringValue;
        }
    }
}
