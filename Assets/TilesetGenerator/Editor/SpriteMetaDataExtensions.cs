using UnityEditor;
using UnityEngine;

namespace TilesetGenerator {
    public static class SpriteMetaDataExtensions
    {
        public static SpriteRect ToSpriteRect(this SpriteMetaData meta) =>
            new()
            {
                name = meta.name,
                rect = meta.rect,
                alignment = (SpriteAlignment)meta.alignment,
                pivot = meta.pivot,
                border = meta.border
            };
    }
}