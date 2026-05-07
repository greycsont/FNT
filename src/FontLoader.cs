using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace FNT;

public static class FontLoader
{
    static readonly Assembly s_fontEngineAssembly = AppDomain.CurrentDomain
        .GetAssemblies().Last(a => a.GetName().Name == "UnityEngine.TextCoreFontEngineModule");
    static readonly Assembly s_tmpAssembly = AppDomain.CurrentDomain
        .GetAssemblies().Last(a => a.GetName().Name == "Unity.TextMeshPro");

    static readonly Type s_fontEngineType = s_fontEngineAssembly
        .GetType("UnityEngine.TextCore.LowLevel.FontEngine");
    static readonly Type s_tmpFontAssetType = s_tmpAssembly
        .GetType("TMPro.TMP_FontAsset");

    static readonly MethodInfo s_initFontEngine = s_fontEngineType
        .GetMethod("InitializeFontEngine", BindingFlags.Public | BindingFlags.Static);
    static readonly MethodInfo s_loadFontFacePath = Array.Find(
        s_fontEngineType.GetMethods(BindingFlags.Public | BindingFlags.Static),
        m => m.Name == "LoadFontFace" && m.GetParameters() is var p
          && p.Length == 1 && p[0].ParameterType == typeof(string));
    static readonly MethodInfo s_setFaceSize = s_fontEngineType
        .GetMethod("SetFaceSize", BindingFlags.Public | BindingFlags.Static);
    static readonly MethodInfo s_getFaceInfo = s_fontEngineType
        .GetMethod("GetFaceInfo", BindingFlags.Public | BindingFlags.Static);

    static readonly BindingFlags s_flags = BindingFlags.NonPublic | BindingFlags.Instance;
    static readonly FieldInfo s_glyphTableField     = s_tmpFontAssetType.GetField("m_GlyphTable",     s_flags);
    static readonly FieldInfo s_freeGlyphRectsField = s_tmpFontAssetType.GetField("m_FreeGlyphRects", s_flags);
    static readonly Type s_glyphType     = s_glyphTableField.FieldType.GetGenericArguments()[0];
    static readonly Type s_glyphRectType = s_freeGlyphRectsField.FieldType.GetGenericArguments()[0];
    static readonly Type s_glyphListType     = typeof(List<>).MakeGenericType(s_glyphType);
    static readonly Type s_glyphRectListType = typeof(List<>).MakeGenericType(s_glyphRectType);

    static readonly MethodInfo s_initDictLookup = s_tmpFontAssetType
        .GetMethod("InitializeDictionaryLookupTables", s_flags);
    static readonly MethodInfo s_addSynthesized = s_tmpFontAssetType
        .GetMethod("AddSynthesizedCharactersAndFaceMetrics", s_flags);

    public static TMP_FontAsset CreateFontAssetFromFile(string path, int samplingPointSize = 90,
        int atlasPadding = 9, int atlasWidth = 512, int atlasHeight = 512)
    {
        s_initFontEngine.Invoke(null, null);

        var loadResult = s_loadFontFacePath.Invoke(null, new object[] { path });
        if (loadResult is int err && err != 0)
        {
            Debug.LogError($"LoadFontFace failed: {loadResult}");
            return null;
        }

        s_setFaceSize.Invoke(null, new object[] { samplingPointSize });
        var faceInfo = s_getFaceInfo.Invoke(null, null);

        var familyName = faceInfo.GetType().GetProperty("familyName")?.GetValue(faceInfo);
        Debug.Log($"FaceInfo familyName: {familyName}, pointSize: {samplingPointSize}");

        var fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
        fontAsset.name = (string)familyName;

        var renderModeType = s_fontEngineAssembly.GetType("UnityEngine.TextCore.LowLevel.GlyphRenderMode");
        var sdfaaMode = Enum.Parse(renderModeType, "SDFAA");

        var popModeType = s_tmpAssembly.GetType("TMPro.AtlasPopulationMode");
        var dynamicMode = Enum.Parse(popModeType, "Dynamic");

        var freeRect = Activator.CreateInstance(s_glyphRectType, new object[] { 0, 0, atlasWidth - 1, atlasHeight - 1 });
        var freeRects = Activator.CreateInstance(s_glyphRectListType);
        s_glyphRectListType.GetMethod("Add").Invoke(freeRects, new[] { freeRect });

        s_tmpFontAssetType.GetField("m_Version",                     s_flags).SetValue(fontAsset, "1.1.0");
        s_tmpFontAssetType.GetField("m_FaceInfo",                    s_flags).SetValue(fontAsset, faceInfo);
        s_tmpFontAssetType.GetField("m_AtlasPopulationMode",         s_flags).SetValue(fontAsset, dynamicMode);
        s_tmpFontAssetType.GetField("m_AtlasWidth",                  s_flags).SetValue(fontAsset, atlasWidth);
        s_tmpFontAssetType.GetField("m_AtlasHeight",                 s_flags).SetValue(fontAsset, atlasHeight);
        s_tmpFontAssetType.GetField("m_AtlasPadding",                s_flags).SetValue(fontAsset, atlasPadding);
        s_tmpFontAssetType.GetField("m_AtlasRenderMode",             s_flags).SetValue(fontAsset, sdfaaMode);
        s_tmpFontAssetType.GetField("m_IsMultiAtlasTexturesEnabled", s_flags).SetValue(fontAsset, true);

        s_tmpFontAssetType.GetField("m_GlyphTable",                  s_flags).SetValue(fontAsset, Activator.CreateInstance(s_glyphListType));
        s_tmpFontAssetType.GetField("m_CharacterTable",              s_flags).SetValue(fontAsset, new List<TMP_Character>());
        s_tmpFontAssetType.GetField("m_UsedGlyphRects",              s_flags).SetValue(fontAsset, Activator.CreateInstance(s_glyphRectListType));
        s_tmpFontAssetType.GetField("m_FreeGlyphRects",              s_flags).SetValue(fontAsset, freeRects);
        s_tmpFontAssetType.GetField("m_GlyphsToRender",              s_flags).SetValue(fontAsset, Activator.CreateInstance(s_glyphListType));
        s_tmpFontAssetType.GetField("m_GlyphsRendered",              s_flags).SetValue(fontAsset, Activator.CreateInstance(s_glyphListType));
        s_tmpFontAssetType.GetField("m_GlyphIndexList",              s_flags).SetValue(fontAsset, new List<uint>());
        s_tmpFontAssetType.GetField("m_GlyphIndexListNewlyAdded",    s_flags).SetValue(fontAsset, new List<uint>());
        s_tmpFontAssetType.GetField("m_GlyphsToAdd",                 s_flags).SetValue(fontAsset, new List<uint>());
        s_tmpFontAssetType.GetField("m_GlyphsToAddLookup",           s_flags).SetValue(fontAsset, new HashSet<uint>());
        s_tmpFontAssetType.GetField("m_CharactersToAdd",             s_flags).SetValue(fontAsset, new List<TMP_Character>());
        s_tmpFontAssetType.GetField("m_CharactersToAddLookup",       s_flags).SetValue(fontAsset, new HashSet<uint>());
        s_tmpFontAssetType.GetField("m_MissingUnicodesFromFontFile", s_flags).SetValue(fontAsset, new HashSet<uint>());

        var atlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.Alpha8, false);
        s_tmpFontAssetType.GetField("m_AtlasTexture",  s_flags).SetValue(fontAsset, atlasTexture);
        s_tmpFontAssetType.GetField("m_AtlasTextures", s_flags).SetValue(fontAsset, new Texture2D[] { atlasTexture });

        var shader = Shader.Find("TextMeshPro/Distance Field");
        var mat    = new Material(shader);
        mat.SetTexture(ShaderUtilities.ID_MainTex,     atlasTexture);
        mat.SetFloat(ShaderUtilities.ID_TextureWidth,  atlasWidth);
        mat.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);
        mat.SetFloat(ShaderUtilities.ID_GradientScale, atlasPadding + 1);
        mat.SetFloat(ShaderUtilities.ID_WeightNormal,  fontAsset.normalStyle);
        mat.SetFloat(ShaderUtilities.ID_WeightBold,    fontAsset.boldStyle);
        fontAsset.material = mat;

        s_initDictLookup?.Invoke(fontAsset, null);
        s_addSynthesized?.Invoke(fontAsset, null);

        var font = CreateFontFromFile(path);
        s_tmpFontAssetType.GetField("m_SourceFontFile", s_flags).SetValue(fontAsset, font);

        return fontAsset;
    }

    public static Font CreateFontFromFile(string path)
    {
        // [Error  : Unity Log] MethodAccessException: Method `UnityEngine.Font.Internal_CreateFontFromPath(UnityEngine.Font,string)' 
        // is inaccessible from method `FontLoader.CreateFontAssetFromFile(string,int,int,int,int)'
        var internalCreateFromPath = typeof(Font).GetMethod(
            "Internal_CreateFontFromPath",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        // GetUninitializedObject == null: True, ReferenceEquals null: False
        // new Font()             == null: False, ReferenceEquals null: False
        var font = (Font)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(Font));

        internalCreateFromPath.Invoke(null, new object[] { font, path });

        return font;
    }
}
