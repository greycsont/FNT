## F.N.T::font_no_tsubasa
A lib to load font to TMP_FontAsset or Font at Unity's runtime

example:

```csharp
TMP_FontAsset font = FNT.FontLoader.CreateFontAssetFromFile(fontPath);
```

```csharp
Font font = FNT.FontLoader.CreateFontFromFile(fontPath);
```

## Credit
naming inspiration: 

Kou! - [A.O.E::area_of_effect](https://osu.ppy.sh/beatmapsets/2321798#mania/4974366)

Camellia - [Kikai No Tsubasa feat. Kanase Teto](https://www.youtube.com/watch?v=vfkC_FaTJiA)

ref:
- Font Engine: https://docs.unity3d.com/ScriptReference/TextCore.LowLevel.FontEngine.html

- Unity's null: 10_days_till_xmas - I LOVE UNITY AND ITS _________ NULL SYSTEM!!!!!

  ![UnityNull](https://raw.githubusercontent.com/greycsont/FNT/master/docs/UnityNull.png)

  ![TestCode](https://raw.githubusercontent.com/greycsont/FNT/master/docs/TestCode.png)
  
  ![ConsoleOutput](https://raw.githubusercontent.com/greycsont/FALLBACKFONT9/master/docs/ConsoleOutput.png)

- decomplie of TMP_FontAsset and Font