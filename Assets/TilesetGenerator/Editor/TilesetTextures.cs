using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Utils = TilesetGenerator.TileGenUtils;

namespace TilesetGenerator {
    public class TilesetTextures
    {
        public Texture2D NwMiniCorner;
        public Texture2D NeMiniCorner;
        public Texture2D SWMiniCorner;
        public Texture2D SeMiniCorner;
        public Texture2D NwMiniInvCorner;
        public Texture2D NeMiniInvCorner;
        public Texture2D SWMiniInvCorner;
        public Texture2D SeMiniInvCorner;
        public Texture2D NwCorner;
        public Texture2D NeCorner;
        public Texture2D SWCorner;
        public Texture2D SeCorner;
        public Texture2D NwInvCorner;
        public Texture2D NeInvCorner;
        public Texture2D SWInvCorner;
        public Texture2D SeInvCorner;
        public Texture2D NShore;
        public Texture2D EShore;
        public Texture2D SShore;
        public Texture2D WShore;
        public Texture2D Island;
        public Texture2D Intersection;
        public Texture2D NsBridge;
        public Texture2D WeBridge;
    
    
        public static async Task<Texture2D> GetInputTextureFromSprites(Sprite nwCornerSprite, Sprite neCornerSprite, Sprite swCornerSprite, Sprite seCornerSprite,
            Sprite nShoreSprite, Sprite eShoreSprite, Sprite sShoreSprite, Sprite wShoreSprite,
            Sprite nwInvCornerSprite, Sprite neInvCornerSprite, Sprite swInvCornerSprite, Sprite seInvCornerSprite,
            Sprite coreSprite,
            int tileSize)
        {
            int ts = tileSize;
            Texture2D outputTex = new(ts * 4, ts * 4)
            {
                filterMode = FilterMode.Point
            };
            await Utils.CopyTexture(outputTex, nwCornerSprite, new(0, outputTex.height - ts));
            await Utils.CopyTexture(outputTex, neCornerSprite, new(outputTex.width - ts * 2, outputTex.height - ts));
            await Utils.CopyTexture(outputTex, swCornerSprite, new(0, ts));
            await Utils.CopyTexture(outputTex, seCornerSprite, new(ts * 2, ts));
            await Utils.CopyTexture(outputTex, nShoreSprite, new(ts, ts * 3));
            await Utils.CopyTexture(outputTex, eShoreSprite, new(ts * 2, ts * 2));
            await Utils.CopyTexture(outputTex, sShoreSprite, new(ts, ts));
            await Utils.CopyTexture(outputTex, wShoreSprite, new(0, ts * 2));
            await Utils.CopyTexture(outputTex, nwInvCornerSprite, new(outputTex.width - ts, outputTex.height - ts));
            await Utils.CopyTexture(outputTex, swInvCornerSprite, new(outputTex.width - ts, outputTex.height - ts * 2));
            await Utils.CopyTexture(outputTex, neInvCornerSprite, new(outputTex.width - ts, outputTex.height - ts * 3));
            await Utils.CopyTexture(outputTex, seInvCornerSprite, new(outputTex.width - ts, outputTex.height - ts * 4));
            await Utils.CopyTexture(outputTex, coreSprite, new(ts, ts * 2));

            outputTex.Apply();
            return outputTex;
        }

        public async Task GenerateSectionsFromInputTexture(Texture2D inputTex, int tileSize, CancellationToken ct = default)
        {
            int ts = tileSize;
            int hs = ts / 2;
            EditorUtility.DisplayProgressBar("Tileset Generation", "Extracting Texture Sections...", 0.2f);
            NwMiniCorner = await Utils.GetTextureCopy(inputTex, new(0, ts * 3 + hs),           hs, hs);
            NeMiniCorner = await Utils.GetTextureCopy(inputTex, new(ts * 2 + hs, ts * 3 + hs), hs, hs);
            SWMiniCorner = await Utils.GetTextureCopy(inputTex, new(0, ts),                    hs, hs);
            SeMiniCorner = await Utils.GetTextureCopy(inputTex, new(ts * 2 + hs, ts),          hs, hs);
            EditorUtility.DisplayProgressBar("Tileset Generation", "Extracting Texture Sections...", 0.3f);
            NwMiniInvCorner = await Utils.GetTextureCopy(inputTex, new(ts * 3, hs),               hs, hs);
            NeMiniInvCorner = await Utils.GetTextureCopy(inputTex, new(ts * 3 + hs, ts * 2 + hs), hs, hs);
            SWMiniInvCorner = await Utils.GetTextureCopy(inputTex, new(ts * 3, ts),               hs, hs);
            SeMiniInvCorner = await Utils.GetTextureCopy(inputTex, new(ts * 3 + hs, ts * 3),      hs, hs);
            EditorUtility.DisplayProgressBar("Tileset Generation", "Extracting Texture Sections...", 0.4f);
            NwCorner = await Utils.GetTextureCopy(inputTex, new(0, inputTex.height - ts),                       ts, ts);
            NeCorner = await Utils.GetTextureCopy(inputTex, new(inputTex.width - ts * 2, inputTex.height - ts), ts, ts);
            SWCorner = await Utils.GetTextureCopy(inputTex, new(0, ts),                                         ts, ts);
            SeCorner = await Utils.GetTextureCopy(inputTex, new(ts * 2, ts),                                    ts, ts);
            EditorUtility.DisplayProgressBar("Tileset Generation", "Extracting Texture Sections...", 0.5f);
            NwInvCorner = await Utils.GetTextureCopy(inputTex, new(inputTex.width - ts, inputTex.height - ts),     ts, ts);
            NeInvCorner = await Utils.GetTextureCopy(inputTex, new(inputTex.width - ts, inputTex.height - ts * 2), ts, ts);
            SWInvCorner = await Utils.GetTextureCopy(inputTex, new(inputTex.width - ts, inputTex.height - ts * 3), ts, ts);
            SeInvCorner = await Utils.GetTextureCopy(inputTex, new(inputTex.width - ts, inputTex.height - ts * 4), ts, ts);
            EditorUtility.DisplayProgressBar("Tileset Generation", "Extracting Texture Sections...", 0.6f);
            NShore = await Utils.GetTextureCopy(inputTex, new(ts, ts * 3),     ts, ts);
            EShore = await Utils.GetTextureCopy(inputTex, new(ts * 2, ts * 2), ts, ts);
            SShore = await Utils.GetTextureCopy(inputTex, new(ts, ts),         ts, ts);
            WShore = await Utils.GetTextureCopy(inputTex, new(0, ts * 2),      ts, ts);
        
            Island = new(ts, ts)
            {
                filterMode = FilterMode.Point
            };
            EditorUtility.DisplayProgressBar("Tileset Generation", "Extracting Texture Sections...", 0.7f);
            await Utils.CopyTexture(Island, NwMiniCorner, new(0, hs));
            await Utils.CopyTexture(Island, NeMiniCorner, new(hs, hs));
            await Utils.CopyTexture(Island, SWMiniCorner, new(0, 0));
            await Utils.CopyTexture(Island, SeMiniCorner, new(hs, 0));
            Intersection = new(ts, ts)
            {
                filterMode = FilterMode.Point
            };
            EditorUtility.DisplayProgressBar("Tileset Generation", "Extracting Texture Sections...", 0.8f);
            await Utils.CopyTexture(Intersection, NwMiniInvCorner, new(0, hs));
            await Utils.CopyTexture(Intersection, NeMiniInvCorner, new(hs, hs));
            await Utils.CopyTexture(Intersection, SWMiniInvCorner, new(0, 0));
            await Utils.CopyTexture(Intersection, SeMiniInvCorner, new(hs, 0));
            NsBridge = new(ts, ts);
            Intersection.filterMode = FilterMode.Point;
            EditorUtility.DisplayProgressBar("Tileset Generation", "Extracting Texture Sections...", 0.9f);
            await Utils.CopyTexture(NsBridge, await Utils.GetTextureCopy(WShore, new(0, 0),   hs, hs), new(0, 0));
            await Utils.CopyTexture(NsBridge, await Utils.GetTextureCopy(EShore, new(hs, 0),  hs, hs), new(hs, 0));
            await Utils.CopyTexture(NsBridge, await Utils.GetTextureCopy(WShore, new(0, hs),  hs, hs), new(0, hs));
            await Utils.CopyTexture(NsBridge, await Utils.GetTextureCopy(EShore, new(hs, hs), hs, hs), new(hs, hs));
            WeBridge = new(ts, ts);
            Intersection.filterMode = FilterMode.Point;
            EditorUtility.DisplayProgressBar("Tileset Generation", "Extracting Texture Sections...", 1f);
            await Utils.CopyTexture(WeBridge, await Utils.GetTextureCopy(NShore, new(hs, hs), hs, hs), new(hs, hs));
            await Utils.CopyTexture(WeBridge, await Utils.GetTextureCopy(SShore, new(hs, 0),  hs, hs), new(hs, 0));
            await Utils.CopyTexture(WeBridge, await Utils.GetTextureCopy(NShore, new(0, hs),  hs, hs), new(0, hs));
            await Utils.CopyTexture(WeBridge, await Utils.GetTextureCopy(SShore, new(0, 0),   hs, hs), new(0, 0));
        }
    }
}