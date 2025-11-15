using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Utils = TilesetGenerator.TileGenUtils;

namespace TilesetGenerator {
    public class TilesetGenerator {
        private const string FULL_PATH = "/TilesetGenerator/Resources/ExportTilemap/";

        private readonly List<Sprite> _spriteList = new();

        private Texture2D _inTex;
        private Texture2D _inputTexFixed;
        private Texture2D _outTex;
        private TilesetGeneratorEditorWindow _tileGenWindow;
        private Texture2D _finalTexture;
        private string _fullOutputPath;
        private string _playerSetName;
        private string _inputFilename;
        private string _outputFolderPath;
        private TilesetTextures _tilesetTextures;

        public static int ts;
        public static int hs;

        private static int Ts2 => ts * 2;
        private static int Ts3 => ts * 3;
        private static int Ts4 => ts * 4;
        private static int Ts5 => ts * 5;
        private static int Ts6 => ts * 6;
        private static int Ts7 => ts * 7;
        private static int Ts8 => ts * 8;
        
        private string AssetPath => $"{Path.Join("Assets", _outputFolderPath, _playerSetName)}{Utils.FILE_TYPE}";
        private string FullAssetPath => $"{Path.Join(Application.dataPath, _outputFolderPath, _playerSetName)}{Utils.FILE_TYPE}";
        private string RuleTilePath => $"{Path.Join("Assets", _outputFolderPath, _playerSetName)}_ruletile.asset";
        private string FullRuleTilePath => $"{Path.Join(Application.dataPath, _outputFolderPath, _playerSetName)}_ruletile.asset";

        public async Task CreateTilesetFromBaseTexture(
                Texture2D input,
                string filename,
                string outputFolderPath,
                TilesetGeneratorEditorWindow tileGenWindow,
                CancellationToken ct = default) {
            _tileGenWindow = tileGenWindow;
            _inTex = input;
            _inputFilename = filename;
            _outputFolderPath = string.IsNullOrWhiteSpace(outputFolderPath) ? FULL_PATH : outputFolderPath.Replace(Application.dataPath, "");
            ts = _inTex.width / 4;
            hs = ts / 2;
            _outTex = new(Ts8, Ts8);
            if (_tileGenWindow) _tileGenWindow.SetOutputTexture(_outTex);
            _outTex.filterMode = FilterMode.Point;
            Utils.CreateGrid(_outTex, ts);
            EditorUtility.DisplayProgressBar("Tileset Generation", "First Pass...", 0.1f);
            _inputTexFixed = new(_inTex.width, _inTex.height);
            _inTex.filterMode = FilterMode.Point;
            Utils.SimpleCopyPaste(_inputTexFixed, _inTex);
            Utils.EraseSquareSectionOfTexture(_inputTexFixed, new(Ts3, 0), ts, ts);

            _tilesetTextures = new();
            GC.Collect();
            await _tilesetTextures.GenerateSectionsFromInputTexture(_inTex, ts, ct);
            await Utils.CopyTexture(_outTex, _inputTexFixed, new(0, Ts4));
            await FirstPass(ct);
            await SecondPass(ct);
            await FinishUp(_inputFilename, ct);
            await Task.Delay(100, ct);
            CreateRuleTile();
        }

        private async Task FirstPass(CancellationToken ct) {
            Func<Task>[] pasteTasks = {
                    () => Utils.CopyTextureSequential(_inTex, new(Ts3, 0), ts, Ts2, _outTex, new(_outTex.width + ts, _outTex.height + Ts6)),
                    () => CopyTexture(_outTex, _tilesetTextures.Island, new(Ts3, Ts5)),
                    () => CopyTexture(_outTex, _tilesetTextures.NwCorner, new(Ts5, Ts7)),
                    () => CopyTexture(_outTex, _tilesetTextures.NeCorner, new(Ts6, Ts7)),
                    () => CopyTexture(_outTex, _tilesetTextures.SWCorner, new(Ts5, Ts6)),
                    () => CopyTexture(_outTex, _tilesetTextures.SeCorner, new(Ts6, Ts6)),
                    () => CopyTexture(_outTex, _tilesetTextures.NShore, new(ts, Ts2)),
                    () => CopyTexture(_outTex, _tilesetTextures.NShore, new(0, ts)),
                    () => CopyTexture(_outTex, _tilesetTextures.EShore, new(Ts2, Ts2)),
                    () => CopyTexture(_outTex, _tilesetTextures.EShore, new(Ts4, Ts2)),
                    () => CopyTexture(_outTex, _tilesetTextures.SShore, new(Ts3, Ts2)),
                    () => CopyTexture(_outTex, _tilesetTextures.SShore, new(Ts5, Ts2)),
                    () => CopyTexture(_outTex, _tilesetTextures.WShore, new(0, Ts2)),
                    () => CopyTexture(_outTex, _tilesetTextures.WShore, new(Ts6, Ts2)),
                    () => CopyTexture(_outTex, _tilesetTextures.NsBridge, new(Ts5, Ts5)),
                    () => CopyTexture(_outTex, _tilesetTextures.NsBridge, new(ts, Ts4)),
                    () => CopyTexture(_outTex, _tilesetTextures.NsBridge, new(Ts3, Ts4)),
                    () => CopyTexture(_outTex, _tilesetTextures.WeBridge, new(Ts6, Ts5)),
                    () => CopyTexture(_outTex, _tilesetTextures.WeBridge, new(0, Ts4)),
                    () => CopyTexture(_outTex, _tilesetTextures.WeBridge, new(Ts2, Ts4)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(Ts4, Ts5)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(Ts4, Ts4)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(Ts5, Ts4)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(Ts6, Ts4)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(0, Ts3)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(ts, Ts3)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(Ts2, Ts3)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(Ts3, Ts3)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(Ts4, Ts3)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(Ts5, Ts3)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(Ts6, Ts3)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(ts, ts)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(Ts2, ts)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(Ts3, ts)),
                    () => CopyTexture(_outTex, _tilesetTextures.Intersection, new(Ts4, ts)),
            };

            var progressStep = pasteTasks.Length > 0
                    ? 1f / pasteTasks.Length
                    : 1;
            foreach (var task in pasteTasks.Select((value, index) => (value, index))) {
                EditorUtility.DisplayProgressBar("Tileset Generation",
                                                 $"First Pass - Copying Texture Sections (Step {task.index + 1}/{pasteTasks.Length})",
                                                 0.1f + Mathf.Lerp(0, 1, task.index * progressStep));
                if (ct.IsCancellationRequested) break;
                await task.value.Invoke();
            }
        }

        private async Task SecondPass(CancellationToken ct = default) {
            Func<Task>[] pasteTasks = {
                    () => CopyTexture(_outTex, _tilesetTextures.SeMiniInvCorner, new(Ts5 + hs, Ts7)),
                    () => CopyTexture(_outTex, _tilesetTextures.SWMiniInvCorner, new(Ts6, Ts7)),
                    () => CopyTexture(_outTex, _tilesetTextures.NeMiniInvCorner, new(Ts5 + hs, Ts6 + hs)),
                    () => CopyTexture(_outTex, _tilesetTextures.NwMiniInvCorner, new(Ts6, Ts6 + hs)),
                    () => CopyTexture(_outTex, _tilesetTextures.NwMiniCorner, new(0, Ts4 + hs)),
                    () => CopyTexture(_outTex, _tilesetTextures.SWMiniCorner, new(0, Ts4)),
                    () => CopyTexture(_outTex, _tilesetTextures.NwMiniCorner, new(ts, Ts4 + hs)),
                    () => CopyTexture(_outTex, _tilesetTextures.NeMiniCorner, new(ts + hs, Ts4 + hs)),
                    () => CopyTexture(_outTex, _tilesetTextures.NeMiniCorner, new(Ts2 + hs, Ts4 + hs)),
                    () => CopyTexture(_outTex, _tilesetTextures.SeMiniCorner, new(Ts2 + hs, Ts4)),
                    () => CopyTexture(_outTex, _tilesetTextures.SWMiniCorner, new(Ts3, Ts4)),
                    () => CopyTexture(_outTex, _tilesetTextures.SeMiniCorner, new(Ts3 + hs, Ts4)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts + hs, Ts2 + hs), hs, hs), new(Ts4 + hs, Ts4 + hs)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts, Ts2), hs, hs), new(Ts4, Ts4)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts, Ts2 + hs), hs, hs), new(Ts5, Ts4 + hs)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts + hs, Ts2), hs, hs), new(Ts5 + hs, Ts4)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts, Ts2), hs, hs), new(Ts6, Ts4)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts + hs, Ts2 + hs), hs, hs), new(Ts4 + hs, Ts3 + hs)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts + hs, Ts2), hs, hs), new(Ts5 + hs, Ts3)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts, Ts2 + hs), hs, hs), new(Ts6, Ts3 + hs)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts, ts), hs, hs), new(0, Ts3)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts + hs, ts), hs, hs), new(hs, Ts3)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(0, Ts2 + hs), hs, hs), new(ts, Ts3 + hs)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(0, Ts2), hs, hs), new(ts, Ts3)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts, Ts3 + hs), hs, hs), new(Ts2, Ts3 + hs)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts + hs, Ts3 + hs), hs, hs), new(Ts2 + hs, Ts3 + hs)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(Ts2 + hs, Ts2 + hs), hs, hs), new(Ts3 + hs, Ts3 + hs)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(Ts2 + hs, Ts2), hs, hs), new(Ts3 + hs, Ts3)),
                    () => CopyTexture(_outTex, _tilesetTextures.NeMiniInvCorner, new(hs, Ts2 + hs)),
                    () => CopyTexture(_outTex, _tilesetTextures.SeMiniInvCorner, new(ts + hs, Ts2)),
                    () => CopyTexture(_outTex, _tilesetTextures.SWMiniInvCorner, new(Ts2, Ts2)),
                    () => CopyTexture(_outTex, _tilesetTextures.NwMiniInvCorner, new(Ts3, Ts2 + hs)),
                    () => CopyTexture(_outTex, _tilesetTextures.NwMiniInvCorner, new(Ts4, Ts2 + hs)),
                    () => CopyTexture(_outTex, _tilesetTextures.NeMiniInvCorner, new(Ts5 + hs, Ts2 + hs)),
                    () => CopyTexture(_outTex, _tilesetTextures.SeMiniInvCorner, new(Ts6 + hs, Ts2)),
                    async () => await CopyTexture(_outTex, _tilesetTextures.SWMiniInvCorner, new(0, ts)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts, Ts2), hs, hs), new(ts, ts)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts + hs, Ts2), hs, hs), new(ts + hs, ts)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts, Ts2 + hs), hs, hs), new(Ts2, ts + hs)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts, Ts2), hs, hs), new(Ts2, ts)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts, Ts2 + hs), hs, hs), new(Ts3, ts + hs)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts + hs, Ts2 + hs), hs, hs), new(Ts3 + hs, ts + hs)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts + hs, Ts2 + hs), hs, hs), new(Ts4 + hs, ts + hs)),
                    async () => await CopyTexture(_outTex, await Utils.GetTextureCopy(_inTex, new(ts + hs, Ts2), hs, hs), new(Ts4 + hs, ts)),
            };

            var progressStep = pasteTasks.Length > 0
                    ? 0.5f / pasteTasks.Length
                    : 0.5f;
            foreach (var task in pasteTasks.Select((value, index) => (value, index))) {
                if (ct.IsCancellationRequested) break;
                await task.value.Invoke();
                EditorUtility.DisplayProgressBar("Tileset Generation",
                                                 $"Second Pass - Copying Texture Sections (Step {task.index + 1}/{pasteTasks.Length})",
                                                 0.1f + Mathf.Lerp(0, 1, task.index * progressStep));
            }
        }

        private async Task FinishUp(string fileName = "", CancellationToken ct = default) {
            var localT = (int)Time.time;
            var localI = Random.Range(100, 999);
            var t = localT.ToString();
            var i = localI.ToString();
            _playerSetName = Utils.GetSanitizedFileName(fileName, t);
            var bytes = _outTex.EncodeToPNG();
            await File.WriteAllBytesAsync(FullAssetPath, bytes, ct);
            AssetDatabase.ActiveRefreshImportMode = AssetDatabase.RefreshImportMode.InProcess;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            await Task.Delay(1000, ct);
            _finalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetPath);
            _tileGenWindow?.RefreshOutputTexture(_finalTexture);
            if (!_finalTexture) {
                Debug.LogError($"Failed to load the generated texture from Resources. Make sure the file was saved correctly.\nPath: {AssetPath}");
                return;
            }

            Utils.SetUpTextureSettings(ref _finalTexture);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Utils.GenerateTileSpriteRects(ref _finalTexture);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("TileGen", $"Tileset generation complete!\n\nGenerated texture can be found at:\n\n{AssetPath}", "OK");
            Selection.activeObject = _finalTexture;
        }

        private void GetChildTiles() {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(AssetPath)
                                       .OfType<Sprite>(); 
            _spriteList.Clear();
            foreach (var sprite in sprites) {
                _spriteList.Add(sprite);
            }

            for (var i = 0; i < _spriteList.Count; i++) {
                _spriteList[i].name = "tile_" + i;
            }
        }

        private static async Task CopyTexture(Texture2D bottomTex, Texture2D topTex, Vector2 coord) {
            var dstX = (int)coord.x;
            var dstY = (int)coord.y;
            Graphics.CopyTexture(topTex, 0, 0, 0, 0, topTex.width, topTex.height,
                                 bottomTex, 0, 0, dstX, dstY);
            bottomTex.Apply();
            await Task.Yield();
        }

        private void CreateRuleTile() {
            GetChildTiles();
            if (_spriteList is not { Count: >= 46 }) {
                Debug.LogError("Sprite list does not contain enough sprites to create a Rule Tile. Expected at least 46 sprites, found " + _spriteList.Count);
                return;
            }

            Debug.Log("Creating Rule tile.");
            if (File.Exists(FullRuleTilePath)) {
                if (EditorUtility.DisplayDialog("TileGen", "A Rule Tile with this name already exists. Do you want to overwrite it?", "Yes", "No")) {
                    File.Delete(FullRuleTilePath);
                    AssetDatabase.Refresh();
                }
                else {
                    return;
                }
            }

            var rTile = ScriptableObject.CreateInstance<RuleTile>();
            AssetDatabase.CreateAsset(rTile, RuleTilePath);
            rTile.m_DefaultSprite = _spriteList[0];
            rTile.m_DefaultSprite = _spriteList[17];
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[33], Utils.R33));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[27], Utils.R27));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[34], Utils.R34));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[32], Utils.R32));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[0], Utils.R0));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[2], Utils.R02));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[16], Utils.R16));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[14], Utils.R14));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[1], Utils.R01));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[43], Utils.R43));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[44], Utils.R44));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[45], Utils.R45));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[46], Utils.R46));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[3], Utils.R03));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[4], Utils.R04));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[5], Utils.R05));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[6], Utils.R06));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[7], Utils.R07));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[8], Utils.R08));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[9], Utils.R09));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[10], Utils.R10));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[11], Utils.R11));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[12], Utils.R12));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[13], Utils.R13));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[15], Utils.R15));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[17], Utils.R17));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[18], Utils.R18));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[19], Utils.R19));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[20], Utils.R20));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[21], Utils.R21));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[23], Utils.R23));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[24], Utils.R24));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[22], Utils.R22));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[25], Utils.R25));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[26], Utils.R26));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[28], Utils.R28));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[29], Utils.R29));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[30], Utils.R30));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[31], Utils.R31));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[35], Utils.R35));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[36], Utils.R36));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[37], Utils.R37));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[38], Utils.R38));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[39], Utils.R39));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[40], Utils.R40));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[41], Utils.R41));
            rTile.m_TilingRules.Add(Utils.CreateTilingRule(_spriteList[42], Utils.R42));

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (EditorUtility.DisplayDialog("TileGen", "Rule Tile created successfully!\nYou can find it at: " + RuleTilePath, "OK")) {
                EditorUtility.SetDirty(rTile);
            }
        }

        private static async Task<T> LoadAsset<T>(string path, CancellationToken ct) where T : UnityEngine.Object {
            if (string.IsNullOrEmpty(path)) return null;
            var resourceLoadOperation = Resources.LoadAsync<Texture2D>(path);
            while (!resourceLoadOperation.isDone && !ct.IsCancellationRequested) {
                await Task.Yield();
            }

            if (ct.IsCancellationRequested) return null;
            return resourceLoadOperation.asset as T;
        }
    }
}