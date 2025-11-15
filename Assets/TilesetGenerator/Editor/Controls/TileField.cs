using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TilesetGenerator.Controls
{
    [UxmlElement]
    public partial class TileField : VisualElement
    {
        public const string PREVIEW_IMAGE_CLASS = "tile-field__preview-image";
        public const string SPRITE_FIELD_CLASS  = "tile-field__sprite-field";
        public const string LABEL_CLASS         = "tile-field__label";
        public const string TILE_FIELD_CLASS    = "tile-field";

        public const string PREVIEW_IMAGE_NAME = "preview-image";
        public const string SPRITE_FIELD_NAME  = "sprite-field";
        public const string TILE_LABEL_NAME    = "tile-label";

        [UxmlAttribute]
        private string TileName {
            get => _tileName;
            set {
                _tileName = value;
                if (TileLabel != null) TileLabel.text = value;
            }
        }

        public Label       TileLabel    { get; private set; }
        public Image       PreviewImage { get; private set; }
        public ObjectField SpriteField  { get; private set; }
        public Sprite      Sprite       { get; private set; }

        private string _tileName = "Tile";
        
        public event System.Action<Sprite> OnSpriteChanged;

        public TileField()
        {
            PreviewImage = new()
                           { name = PREVIEW_IMAGE_NAME };
            PreviewImage.AddToClassList(PREVIEW_IMAGE_CLASS);
            AddToClassList(TILE_FIELD_CLASS);
            TileLabel = new()
                        {
                            name = TILE_LABEL_NAME,
                            text = TileName, 
                            enabledSelf = false,
                        };
            TileLabel.AddToClassList(LABEL_CLASS);
            PreviewImage.Add(TileLabel);

            SpriteField = new()
                          { name = SPRITE_FIELD_NAME, objectType = typeof(Sprite) };
            SpriteField.AddToClassList(SPRITE_FIELD_CLASS);
            SpriteField.RegisterValueChangedCallback(OnSpriteFieldChanged);
            PreviewImage.Add(SpriteField);
            Add(PreviewImage);
        }

        private void OnSpriteFieldChanged(ChangeEvent<Object> evt)
        {
            Sprite = evt.newValue as Sprite;
            PreviewImage.sprite = Sprite;
            OnSpriteChanged?.Invoke(Sprite);
        }

        public void SetSprite(Sprite sprite)
        {
            Sprite = sprite;
            if (SpriteField != null) SpriteField.value = sprite;
            if (PreviewImage != null) PreviewImage.sprite = sprite;
        }
    }
}