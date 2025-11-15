using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TilesetGenerator {
    public static class TileGenUtils
    {
        public const string FILE_TYPE = ".png";

        private static readonly HashSet<char> FORBIDDEN_CHARS = new()
        {
            ' ',
            '!',
            '\"',
            '#',
            '$',
            '%',
            '&',
            '\'',
            '(',
            ')',
            '*',
            '+',
            ',',
            '-',
            '.',
            '/',
            ':',
            ';',
            '<',
            '=',
            '>',
            '?',
            '@',
            '[',
            '\\',
            ']',
            '^',
            '`',
            '{',
            '|',
            '}',
            '~'
        };

        public static void SetUpTextureSettings(ref Texture2D texture)
        {
            if (!texture) return;
            var assetPath = AssetDatabase.GetAssetPath(texture);
            var assetImporter = AssetImporter.GetAtPath(assetPath);
            var textureImporter = (TextureImporter)assetImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Multiple;
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.spritePixelsPerUnit = (int)(TilesetGenerator.ts * (1 + 21f / 64f));
            var textureSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            textureImporter.SetTextureSettings(textureSettings);
            assetImporter.SaveAndReimport();
        }

        public static bool GenerateTileSpriteRects(ref Texture2D texture)
        {
            if (!texture) return false;
            var assetPath = AssetDatabase.GetAssetPath(texture);
            var assetImporter = AssetImporter.GetAtPath(assetPath);
            if (!assetImporter) return false;
            var spriteSize = TilesetGenerator.ts;
            var count = 8;
            var metas = new List<SpriteMetaData>();
            var metasExtras = new List<SpriteMetaData>();
            var finalMetas = new List<SpriteMetaData>();
            var i = -8;
            for (var y = count; y > 0; --y) {
                for (var x = 0; x < count; ++x) {
                    var meta = new SpriteMetaData
                    {
                        rect = new(x * spriteSize, y * spriteSize - spriteSize, spriteSize, spriteSize),
                        name = "a"
                    };
                    i++;

                    metas.Add(meta);
                }
            }

            for (var z = metas.Count; z > 0; z--) {
                if (z is not (63 or 62 or 61 or 60 or 59 or 58 or 57 or 56 or 55 or 54 or 53 or 47 or 39 or 31 or 23 or 15 or 7)) continue;
                var tempData = metas[z];
                tempData.name = $"other_{z}";
                metas[z] = tempData;
                metasExtras.Add(tempData);
                metas.RemoveAt(z);
            }

            for (var u = 0; u < metas.Count; u++) {
                var tempData = metas[u];
                tempData.name = u.ToString();
                finalMetas.Add(tempData);
            }

            finalMetas.AddRange(metasExtras);
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var spriteDataProvider = factory.GetSpriteEditorDataProviderFromObject(assetImporter);
            spriteDataProvider.InitSpriteEditorDataProvider();
            spriteDataProvider.SetSpriteRects(finalMetas.Select(meta => meta.ToSpriteRect()).ToArray());
            spriteDataProvider.Apply();
            assetImporter.SaveAndReimport();
            return true;
        }

        public static void EraseSquareSectionOfTexture(Texture2D bottomTex, Vector2 coord, int width, int height)
        {
            for (var x = (int)coord.x; x < width + (int)coord.x; x++) {
                for (var y = (int)coord.y; y < height + (int)coord.y; y++) {
                    bottomTex.SetPixel(x, y, new(0, 0, 0, 0));
                }
            }

            bottomTex.Apply();
        }

        public static async Task CopyTexture(Texture2D bottomTex, Texture2D topTex, Vector2 coord)
        {
            var dstX = Mathf.Clamp((int)coord.x, 0, Mathf.Abs(bottomTex.width - topTex.width));
            var dstY = Mathf.Clamp((int)coord.y, 0, Mathf.Abs(bottomTex.height - topTex.height));
            coord = new(dstX, dstY);
            Graphics.CopyTexture(topTex, 0, 0, 0, 0, topTex.width, topTex.height,
                                 bottomTex, 0, 0, (int)coord.x, (int)coord.y);
            await Task.Yield();
        }
    
        public static async Task CopyTexture(Texture2D bottomTex, Sprite topSprite, Vector2 bottomCoord)
        {
            var topCoord = topSprite.textureRect.position;
            var width = (int)topSprite.textureRect.width;
            var height = (int)topSprite.textureRect.height;
            var dstX = Mathf.Clamp((int)bottomCoord.x, 0, Mathf.Abs(bottomTex.width - width));
            var dstY = Mathf.Clamp((int)bottomCoord.y, 0, Mathf.Abs(bottomTex.height - height));
            bottomCoord = new(dstX, dstY);
            Graphics.CopyTexture(topSprite.texture, 0, 0, (int)topCoord.x, (int)topCoord.y, width, height,
                                 bottomTex, 0, 0, (int)bottomCoord.x, (int)bottomCoord.y);
            await Task.Yield();
        }

        public static async Task<Texture2D> GetTextureCopy(Texture2D bottomTex, Vector2 coord, int width, int height)
        {
            var output = new Texture2D(width, height, TextureFormat.ARGB32, false);
            bottomTex.Apply();
            var dstX = Mathf.Clamp((int)coord.x, 0, bottomTex.width - width);
            var dstY = Mathf.Clamp((int)coord.y, 0, bottomTex.height - height);
            coord = new(dstX, dstY);
            Graphics.CopyTexture(bottomTex, 0, 0, (int)coord.x, (int)coord.y, width, height,
                                 output, 0, 0, 0, 0);
            output.Apply();
            await Task.Yield();
            return output;
        }

        public static void CreateGrid(Texture2D where, int tileSize)
        {
            var currentRow = 0;
            var rowCount = 0;

            var currentColumn = 0;
            var columnCount = 0;

            for (var x = 0; x < where.width; x++) {
                for (var y = 0; y < where.height; y++) {
                    if (y == currentRow * tileSize) {
                        where.SetPixel(x, y, Color.grey);
                        rowCount++;
                        if (rowCount > 0) {
                            rowCount = 0;
                            currentRow++;
                            if (currentRow >= 8) {
                                currentRow = 0;
                            }
                        }
                    }

                    if (x != currentColumn * tileSize) continue;
                    where.SetPixel(x, y, Color.grey);
                    columnCount++;
                    if (columnCount < where.height) continue;
                    columnCount = 0;
                    currentColumn++;
                }
            }

            where.Apply();
        }

        public static async Task CopyTextureSequential(Texture2D reference,
            Vector2   refCoord,
            int       width, int height,
            Texture2D targetTexture,
            Vector2   targetCoord)
        {
            for (var x = (int)refCoord.x; x < refCoord.x + width; x++) {
                for (var y = (int)refCoord.y; y < refCoord.y + height; y++) {
                    var col = reference.GetPixel(x, y);
                    targetTexture.SetPixel((int)targetCoord.x + x, (int)targetCoord.y + y, col);
                }

                await Task.Yield();
            }

            targetTexture.Apply();
        }

        public static void SimpleCopyPaste(Texture2D bottomTex, Texture2D topTex)
        {
            for (var x = 0; x < topTex.width; x++) {
                for (var y = 0; y < topTex.height; y++) {
                    bottomTex.SetPixel(x, y, topTex.GetPixel(x, y));
                }
            }

            bottomTex.Apply();
        }

        public static string GetSanitizedFileName(string fileName, string t)
        {
            string name;
            if (!string.IsNullOrEmpty(fileName)) {
                var s = fileName;
                foreach (var c in s.Where(c => FORBIDDEN_CHARS.Contains(c))) {
                    Debug.LogWarning($"Forbidden character '{c}' found in the set name. Replacing with underscore.");
                    s = s.Replace(c, '_');
                }

                name = s.Contains(t) ? $"Txtr_{Random.Range(100, 9999)}_" : $"{s}";
            }
            else {
                name = "Txtr_" + Random.Range(100, 9999) + "_";
            }

            return name;
        }

        public static RuleTile.TilingRule CreateTilingRule(Sprite sprite, IEnumerable<int> neighbors) =>
            new()
            {
                m_Sprites =
                {
                    [0] = sprite
                },
                m_Neighbors = neighbors.ToList()
            };

        public static readonly int[] R01 =
        {
            0, 2, 0,
            1, 1,
            1, 1, 1
        };

        public static readonly int[] R03 =
        {
            1, 1, 0,
            1, 1,
            0, 1, 2
        };

        public static readonly int[] R04 =
        {
            0, 1, 1,
            1, 1,
            2, 1, 0
        };

        public static readonly int[] R05 =
        {
            0, 2, 0,
            2, 1,
            0, 1, 2
        };

        public static readonly int[] R06 =
        {
            0, 2, 0,
            1, 2,
            2, 1, 0
        };

        public static readonly int[] R07 =
        {
            0, 1, 1,
            2, 1,
            0, 1, 1
        };

        public static readonly int[] R11 =
        {
            2, 1, 0,
            1, 1,
            0, 1, 1
        };

        public static readonly int[] R12 =
        {
            0, 1, 2,
            2, 1,
            0, 2, 0
        };

        public static readonly int[] R13 =
        {
            2, 1, 0,
            1, 2,
            0, 2, 0
        };

        public static readonly int[] R15 =
        {
            1, 1, 1,
            1, 1,
            0, 2, 0
        };

        public static readonly int[] R17 =
        {
            0, 2, 0,
            2, 2,
            0, 2, 0
        };

        public static readonly int[] R18 =
        {
            2, 1, 2,
            1, 1,
            2, 1, 2
        };

        public static readonly int[] R19 =
        {
            0, 1, 0,
            2, 2,
            0, 1, 0
        };

        public static readonly int[] R20 =
        {
            0, 2, 0,
            1, 1,
            0, 2, 0
        };

        public static readonly int[] R21 =
        {
            0, 2, 0,
            2, 1,
            0, 2, 0
        };

        public static readonly int[] R22 =
        {
            0, 2, 0,
            2, 2,
            0, 1, 0
        };

        public static readonly int[] R23 =
        {
            0, 2, 0,
            1, 2,
            0, 2, 0
        };

        public static readonly int[] R24 =
        {
            0, 1, 0,
            2, 2,
            0, 2, 0
        };

        public static readonly int[] R25 =
        {
            2, 1, 1,
            1, 1,
            1, 1, 2
        };

        public static readonly int[] R26 =
        {
            1, 1, 2,
            1, 1,
            2, 1, 1
        };

        public static readonly int[] R27 =
        {
            2, 1, 2,
            1, 1,
            1, 1, 2
        };

        public static readonly int[] R28 =
        {
            2, 1, 2,
            1, 1,
            0, 2, 0
        };

        public static readonly int[] R29 =
        {
            0, 1, 2,
            2, 1,
            0, 1, 2
        };

        public static readonly int[] R30 =
        {
            0, 2, 0,
            1, 1,
            2, 1, 2
        };

        public static readonly int[] R31 =
        {
            2, 1, 0,
            1, 2,
            2, 1, 0
        };

        public static readonly int[] R34 =
        {
            1, 1, 2,
            1, 1,
            2, 1, 2
        };

        public static readonly int[] R35 =
        {
            0, 1, 2,
            2, 1,
            0, 1, 1
        };

        public static readonly int[] R36 =
        {
            0, 2, 0,
            1, 1,
            1, 1, 2
        };

        public static readonly int[] R37 =
        {
            1, 1, 0,
            1, 2,
            2, 1, 0
        };

        public static readonly int[] R38 =
        {
            2, 1, 1,
            1, 1,
            0, 2, 0
        };

        public static readonly int[] R39 =
        {
            2, 1, 0,
            1, 2,
            1, 1, 0
        };

        public static readonly int[] R40 =
        {
            1, 1, 2,
            1, 1,
            0, 2, 0
        };

        public static readonly int[] R41 =
        {
            0, 1, 1,
            2, 1,
            0, 1, 2
        };

        public static readonly int[] R42 =
        {
            0, 2, 0,
            1, 1,
            2, 1, 1
        };

        public static readonly int[] R32 =
        {
            2, 1, 1,
            1, 1,
            2, 1, 2
        };

        public static readonly int[] R0 =
        {
            0, 2, 0,
            2, 1,
            0, 1, 1
        };

        public static readonly int[] R33 =
        {
            2, 1, 2,
            1, 1,
            2, 1, 1
        };

        public static readonly int[] R02 =
        {
            0, 2, 0,
            1, 2,
            1, 1, 0
        };

        public static readonly int[] R08 =
        {
            1, 1, 1,
            1, 1,
            1, 1, 1
        };

        public static readonly int[] R09 =
        {
            1, 1, 0,
            1, 2,
            1, 1, 0
        };

        public static readonly int[] R10 =
        {
            0, 1, 2,
            1, 1,
            1, 1, 0
        };

        public static readonly int[] R14 =
        {
            0, 1, 1,
            2, 1,
            0, 2, 0
        };

        public static readonly int[] R16 =
        {
            1, 1, 0,
            1, 2,
            0, 2, 0
        };

        public static readonly int[] R43 =
        {
            2, 1, 2,
            1, 1,
            1, 1, 1
        };

        public static readonly int[] R44 =
        {
            1, 1, 2,
            1, 1,
            1, 1, 2
        };

        public static readonly int[] R45 =
        {
            1, 1, 1,
            1, 1,
            2, 1, 2
        };

        public static readonly int[] R46 =
        {
            2, 1, 1,
            1, 1,
            2, 1, 1
        };
    }
}