using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using TilesetGenerator.Controls;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Image = UnityEngine.UIElements.Image;

namespace TilesetGenerator {
    [SuppressMessage("ReSharper", "HeapView.ClosureAllocation")]
    public class TilesetGeneratorEditorWindow : EditorWindow {
        private const string USS_PATH = "Assets/TilesetGenerator/Editor/TilesetGeneratorEditorWindow.uss";

        private const int TEX_OBJ_SIZE = 76;

        [SerializeField] private string inputFilename = string.Empty;

        private Texture2D _inputTexture;
        private Texture2D _convertedInputTexture;
        private Texture2D _outputTexture;
        private TilesetGenerator _tilePacker;

        private Vector2 _scrollPosition;
        private bool _validInputSprites = false;

        private Texture2D _cornersLayoutTexture;
        private Texture2D _invCornersLayoutTexture;

        [SerializeField] private Sprite nwCornerSprite;
        [SerializeField] private Sprite neCornerSprite;
        [SerializeField] private Sprite swCornerSprite;
        [SerializeField] private Sprite seCornerSprite;
        [SerializeField] private Sprite nShoreSprite;
        [SerializeField] private Sprite eShoreSprite;
        [SerializeField] private Sprite sShoreSprite;
        [SerializeField] private Sprite wShoreSprite;
        [SerializeField] private Sprite nwInvCornerSprite;
        [SerializeField] private Sprite neInvCornerSprite;
        [SerializeField] private Sprite swInvCornerSprite;
        [SerializeField] private Sprite seInvCornerSprite;
        [SerializeField] private Sprite coreSprite;

        private bool _generatingTileset = false;

        private Label _spriteStatusLabel;
        private Label _spritesheetStatusLabel;

        private Button _generateFromSpritesheetButton;
        private Button _generateFromSpritesButton;

        private Image _outputTexturePreviewImage;

        private CancellationTokenSource _cancellationTokenSource = new();

        [SerializeField] private VisualTreeAsset mVisualTreeAsset;

        private void OnEnable() {
            mVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/TilesetGenerator/Editor/TilesetGeneratorEditorWindow.uxml");
        }

        [MenuItem("Tools/Tileset Generator")]
        public static void ShowExample() {
            TilesetGeneratorEditorWindow wnd = GetWindow<TilesetGeneratorEditorWindow>();
            wnd.titleContent = new("Tileset Generator");
        }

        public void CreateGUI() {
            VisualElement root = rootVisualElement;
            VisualElement labelFromUxml = mVisualTreeAsset?.CloneTree();
            root.Add(labelFromUxml);
            root.Bind(new(this));
            _spritesheetStatusLabel = root.Q<Label>("generation-info-spritesheet");
            _spriteStatusLabel = root.Q<Label>("generation-info-sprites");
            root.Query<TileField>().ForEach(tile =>
            {
                string[] nameParts = tile.name.Split('-');
                if (nameParts.Length < 2) return;
                tile.SetSprite(GetSpriteByName(tile.name));
                var highlightElement = root.Q<VisualElement>(string.Join("-", nameParts[..2]) + "-highlight");
                if (highlightElement == null) return;
                tile.RegisterCallback<PointerEnterEvent, VisualElement>((_, vs) => vs.style.opacity = 1, highlightElement);
                tile.RegisterCallback<PointerLeaveEvent, VisualElement>((_, vs) => vs.style.opacity = 0, highlightElement);
                tile.OnSpriteChanged += sprite =>
                {
                    SetSpriteByName(tile.name, sprite);
                    UpdateSpritesStatus();
                };
            });

            _outputTexturePreviewImage = root.Q<Image>("output-image");
            if (_outputTexturePreviewImage != null) _outputTexturePreviewImage.image = _outputTexture;
            root.Q<Button>("generate-tileset-spritesheet-button").clicked += async () =>
            {
                if (_generatingTileset) return;
                try {
                    await GenerateTilesetFromSpritesheetAsync();
                }
                catch (Exception e) {
                    EditorUtility.DisplayDialog("Tileset Generation Error", $"An error occurred during tileset generation:\n{e.Message}", "OK");
                }
            };

            _generateFromSpritesButton = root.Q<Button>("generate-tileset-sprites-button");
            if (_generateFromSpritesButton != null) {
                _generateFromSpritesButton.clicked += async () =>
                {
                    if (_generatingTileset) return;
                    try {
                        await GenerateTilesetFromSpritesAsync();
                    }
                    catch (Exception e) {
                        EditorUtility.DisplayDialog("Tileset Generation Error", $"An error occurred during tileset generation:\n{e.Message}", "OK");
                    }
                };
            }

            UpdateSpritesStatus();
        }

        private void UpdateSpritesStatus() {
            _validInputSprites = ValidateInputSprites();
            _generateFromSpritesButton?.SetEnabled(_validInputSprites);
            if (_spriteStatusLabel == null) return;
            _spriteStatusLabel.text = _validInputSprites
                ? "All required sprites are assigned."
                : "Please assign all required sprites.";
        }

        private async Task GenerateInputTextureAsync() => _convertedInputTexture = await TilesetTextures.GetInputTextureFromSprites(
            nwCornerSprite,
            neCornerSprite,
            swCornerSprite,
            seCornerSprite,
            nShoreSprite,
            eShoreSprite,
            sShoreSprite,
            wShoreSprite,
            nwInvCornerSprite,
            neInvCornerSprite,
            swInvCornerSprite,
            seInvCornerSprite,
            coreSprite,
            (int)neCornerSprite.rect.width);

        private void SetSpriteByName(string fieldName, Sprite sprite) {
            switch (fieldName) {
                case "nw-corner-tile": nwCornerSprite = sprite; break;
                case "ne-corner-tile": neCornerSprite = sprite; break;
                case "sw-corner-tile": swCornerSprite = sprite; break;
                case "se-corner-tile": seCornerSprite = sprite; break;
                case "n-corner-tile": nShoreSprite = sprite; break;
                case "e-corner-tile": eShoreSprite = sprite; break;
                case "s-corner-tile": sShoreSprite = sprite; break;
                case "w-corner-tile": wShoreSprite = sprite; break;
                case "nw-inv-corner-tile": nwInvCornerSprite = sprite; break;
                case "ne-inv-corner-tile": neInvCornerSprite = sprite; break;
                case "sw-inv-corner-tile": swInvCornerSprite = sprite; break;
                case "se-inv-corner-tile": seInvCornerSprite = sprite; break;
                case "core-corner-tile": coreSprite = sprite; break;
                default: Debug.LogWarning($"Unknown sprite field name: {fieldName}"); break;
            }
        }

        private Sprite GetSpriteByName(string fieldName) =>
            fieldName switch {
                "nw-corner-tile" => nwCornerSprite,
                "ne-corner-tile" => neCornerSprite,
                "sw-corner-tile" => swCornerSprite,
                "se-corner-tile" => seCornerSprite,
                "n-corner-tile" => nShoreSprite,
                "e-corner-tile" => eShoreSprite,
                "s-corner-tile" => sShoreSprite,
                "w-corner-tile" => wShoreSprite,
                "nw-inv-corner-tile" => nwInvCornerSprite,
                "ne-inv-corner-tile" => neInvCornerSprite,
                "sw-inv-corner-tile" => swInvCornerSprite,
                "se-inv-corner-tile" => seInvCornerSprite,
                "core-corner-tile" => coreSprite,
                _ => null,
            };

        public void SetOutputTexture(Texture2D outputTex) => _outputTexture = outputTex;

        private async Task GenerateTilesetFromSpritesAsync() {
            _tilePacker ??= new();
            if (_outputTexturePreviewImage != null) _outputTexturePreviewImage.image = _outputTexture;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new();
            try {
                _generatingTileset = true;

                await GenerateInputTextureAsync();
                if (!_convertedInputTexture) {
                    Debug.LogError("ConvertedInputTexture is null!");
                    return;
                }

                await _tilePacker.CreateTilesetFromBaseTexture(_convertedInputTexture, inputFilename, this, _cancellationTokenSource.Token);
            }
            catch (Exception e) {
                Debug.LogError($"Error during tileset generation: {e.Message}");
                EditorUtility.DisplayDialog("Tileset Generation Error", $"An error occurred during tileset generation:\n{e.Message}", "OK");
            }
            finally {
                EditorUtility.ClearProgressBar();
                _generatingTileset = false;
            }
        }

        private async Task GenerateTilesetFromSpritesheetAsync() {
            _tilePacker ??= new();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new();
            try {
                _generatingTileset = true;

                if (!_inputTexture) {
                    Debug.LogError("InputTexture is null!");
                    return;
                }

                await _tilePacker.CreateTilesetFromBaseTexture(_inputTexture, inputFilename, this, _cancellationTokenSource.Token);
            }
            catch (Exception e) {
                Debug.LogError($"Error during tileset generation: {e.Message}");
                EditorUtility.DisplayDialog("Tileset Generation Error", $"An error occurred during tileset generation:\n{e.Message}", "OK");
            }
            finally {
                EditorUtility.ClearProgressBar();
                _generatingTileset = false;
            }
        }

        private bool ValidateInputSprites() =>
            nwCornerSprite
            && neCornerSprite
            && swCornerSprite
            && seCornerSprite
            && nShoreSprite
            && eShoreSprite
            && sShoreSprite
            && wShoreSprite
            && nwInvCornerSprite
            && neInvCornerSprite
            && swInvCornerSprite
            && seInvCornerSprite
            && coreSprite;
    }
}