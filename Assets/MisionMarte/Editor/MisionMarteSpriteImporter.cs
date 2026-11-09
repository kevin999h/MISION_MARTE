using UnityEditor;
using UnityEngine;

namespace MisionMarte.EditorTools
{
    /// <summary>
    /// Fuerza que TODAS las imágenes de Assets/MisionMarte/Arte se importen como Sprite pixel art
    /// (Sprite 2D and UI, 16 píxeles por unidad, sin filtro, sin compresión). Así nunca quedan casillas en "None".
    /// </summary>
    public class MisionMarteSpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/MisionMarte/Arte/")) return;
            var imp = (TextureImporter)assetImporter;
            bool logo = assetPath.EndsWith("Logo.png");
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = logo ? 100 : 16;
            imp.filterMode = logo ? FilterMode.Bilinear : FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            imp.SetTextureSettings(s);
        }
    }
}
