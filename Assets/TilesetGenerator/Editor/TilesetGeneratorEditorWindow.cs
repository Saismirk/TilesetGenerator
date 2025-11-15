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
        [SerializeField] private string outputFolderPath = string.Empty;

        private Texture2D _inputTexture;
        private Texture2D _convertedInputTexture;
        private Texture2D _outputTexture;
        private TilesetGenerator _tilePacker;

        private Vector2 _scrollPosition;
        private bool _validInputSpriteSheet = false;

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

        [SerializeField] private Texture2D spriteSheet;

        private bool _generatingTileset = false;

        private Label _spriteStatusLabel;
        private Image _spriteStatusIcon;
        private Image _outputTexturePreviewImage;
        private Button _generateTilesetButton;
        private Button _browseFolderButton;
        private TabView _tabs;

        private CancellationTokenSource _cancellationTokenSource = new();

        [SerializeField] private VisualTreeAsset mVisualTreeAsset;

        private string SelectedTab => _tabs?.activeTab?.label ?? "";

        private void OnEnable() {
            mVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/TilesetGenerator/Editor/TilesetGeneratorEditorWindow.uxml");
        }

        [MenuItem("Tools/Tileset Generator")]
        public static void ShowExample() {
            var wnd = GetWindow<TilesetGeneratorEditorWindow>();
            wnd.titleContent = new("Tileset Generator");
        }

        public void CreateGUI() {
            var root = rootVisualElement;
            VisualElement labelFromUxml = mVisualTreeAsset?.CloneTree();
            root.Add(labelFromUxml);
            root.Bind(new(this));
            _spriteStatusLabel = root.Q<Label>("generation-info-sprites");
            _spriteStatusIcon = root.Q<Image>("info-icon");
            _tabs = root.Q<TabView>("tabs");
            if (_tabs != null) _tabs.activeTabChanged += (_, _) => GetInputValidation()?.Invoke();
            root.Query<TileField>()
                .ForEach(tile =>
                {
                    var nameParts = tile.name.Split('-');
                    if (nameParts.Length < 2) return;
                    tile.SetSprite(GetSpriteByName(tile.name));
                    var highlightElement = root.Q<VisualElement>(string.Join("-", nameParts[..2]) + "-highlight");
                    if (highlightElement == null) return;
                    tile.RegisterCallback<PointerEnterEvent, VisualElement>((_, vs) => vs.style.opacity = 1, highlightElement);
                    tile.RegisterCallback<PointerLeaveEvent, VisualElement>((_, vs) => vs.style.opacity = 0, highlightElement);
                    tile.OnSpriteChanged += sprite =>
                    {
                        SetSpriteByName(tile.name, sprite);
                        GetInputValidation()?.Invoke();
                    };
                });

            var spritesheetInputField = root.Q<ObjectField>("input-spritesheet-field");
            spritesheetInputField?.RegisterValueChangedCallback(evt =>
            {
                spriteSheet = evt.newValue as Texture2D;
                var inputImage = root.Q<Image>("input-image");
                if (inputImage != null) inputImage.image = spriteSheet; 
                GetInputValidation()?.Invoke();
            });

            _outputTexturePreviewImage = root.Q<Image>("output-image");
            if (_outputTexturePreviewImage != null) _outputTexturePreviewImage.image = _outputTexture;
            _generateTilesetButton = root.Q<Button>("generate-tileset-sprites-button");
            if (_generateTilesetButton != null) {
                _generateTilesetButton.clicked += async () =>
                {
                    if (_generatingTileset) return;
                    try {
                        await (GetTilesetGenerationTask()?.Invoke() ?? Task.CompletedTask);
                    }
                    catch (Exception e) {
                        EditorUtility.DisplayDialog("Tileset Generation Error", $"An error occurred during tileset generation:\n{e.Message}", "OK");
                    }
                };
            }

            _browseFolderButton = root.Q<Button>("browse-folder-button");
            if (_browseFolderButton != null) {
                _browseFolderButton.clicked += () =>
                {
                    var folderPath = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets/TilesetGenerator/ExportTilemap", "");
                    if (!string.IsNullOrEmpty(folderPath)) outputFolderPath = folderPath;
                };
            }

            GetInputValidation()?.Invoke();
        }

        private Action GetInputValidation() => SelectedTab switch {
                "Sprites Input"     => UpdateSpritesStatus,
                "Spritesheet Input" => UpdateSpriteSheetStatus,
                _                   => null,
        };
        
        private Func<Task> GetTilesetGenerationTask() => SelectedTab switch {
                "Sprites Input"     => GenerateTilesetFromSpritesAsync,
                "Spritesheet Input" => GenerateTilesetFromSpritesheetAsync,
                _                   => null,
        };

        private void UpdateSpritesStatus() {
            _validInputSpriteSheet = ValidateInputSprites();
            _generateTilesetButton?.SetEnabled(_validInputSpriteSheet);
            if (_spriteStatusLabel != null)
                _spriteStatusLabel.text = _validInputSpriteSheet
                        ? "All required sprites are assigned."
                        : "Please assign all required sprites.";
            if (_spriteStatusIcon != null)
                _spriteStatusIcon.image = _validInputSpriteSheet
                        ? EditorGUIUtility.IconContent("console.infoicon").image
                        : EditorGUIUtility.IconContent("console.warnicon").image;
        }

        private void UpdateSpriteSheetStatus() {
            _validInputSpriteSheet = ValidateInputSpriteSheet();
            _generateTilesetButton?.SetEnabled(_validInputSpriteSheet);
            if (_spriteStatusLabel != null)
                _spriteStatusLabel.text = _validInputSpriteSheet
                        ? "Required sprite sheet is valid."
                        : "Please assign an input sprite sheet.";
            if (_spriteStatusIcon != null)
                _spriteStatusIcon.image = _validInputSpriteSheet
                        ? EditorGUIUtility.IconContent("console.infoicon").image
                        : EditorGUIUtility.IconContent("console.warnicon").image;
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
                        "nw-corner-tile"     => nwCornerSprite,
                        "ne-corner-tile"     => neCornerSprite,
                        "sw-corner-tile"     => swCornerSprite,
                        "se-corner-tile"     => seCornerSprite,
                        "n-corner-tile"      => nShoreSprite,
                        "e-corner-tile"      => eShoreSprite,
                        "s-corner-tile"      => sShoreSprite,
                        "w-corner-tile"      => wShoreSprite,
                        "nw-inv-corner-tile" => nwInvCornerSprite,
                        "ne-inv-corner-tile" => neInvCornerSprite,
                        "sw-inv-corner-tile" => swInvCornerSprite,
                        "se-inv-corner-tile" => seInvCornerSprite,
                        "core-corner-tile"   => coreSprite,
                        _                    => null,
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

                await _tilePacker.CreateTilesetFromBaseTexture(_convertedInputTexture, inputFilename, outputFolderPath, this, _cancellationTokenSource.Token);
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
            _inputTexture = spriteSheet;
            try {
                _generatingTileset = true;

                if (!_inputTexture) {
                    Debug.LogError("InputTexture is null!");
                    return;
                }

                await _tilePacker.CreateTilesetFromBaseTexture(_inputTexture, inputFilename, outputFolderPath, this, _cancellationTokenSource.Token);
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

        private bool ValidateInputSpriteSheet() =>
                spriteSheet
                && spriteSheet.width > 0
                && spriteSheet.height == spriteSheet.width 
                && spriteSheet.width == Mathf.NextPowerOfTwo(spriteSheet.width);

        public void RefreshOutputTexture(Texture2D finalTexture) => _outputTexturePreviewImage.image = finalTexture;
    }
}