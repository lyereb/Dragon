//帮助类
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Dragon.Resource
{
    public class SpriteLoader
    {
        #region Singleton
        private static SpriteLoader _instance;
        public static SpriteLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SpriteLoader();
                }
                return _instance;
            }
        }
        #endregion

        private const string BufferSpriteResourcesPath = "Sprites/Common/SpriteBuffer";

        private Sprite _bufferSprite;

        public void Init()
        {
            _bufferSprite = Resources.Load<Sprite>(BufferSpriteResourcesPath);
        }

        public void SetImage(Image image, string path, Action<Sprite> onComplete = null)
        {
            image.sprite = _bufferSprite;
            ResourceManager.Instance.LoadAsync<Sprite>(path, (sprite) =>
            {
                image.sprite = sprite;
                onComplete?.Invoke(sprite);
            }, (exception) =>
            {
                Debug.LogError($"LoadSpriteAsync failed: {exception.Message}");
            });
        }

        public void SetSpriteRenderer(SpriteRenderer spriteRenderer, string path, Action<Sprite> onComplete = null)
        {
            spriteRenderer.sprite = _bufferSprite;
            ResourceManager.Instance.LoadAsync<Sprite>(path, (sprite) =>
            {
                spriteRenderer.sprite = sprite;
                onComplete?.Invoke(sprite);
            }, (exception) =>
            {
                Debug.LogError($"LoadSpriteAsync failed: {exception.Message}");
            });
        }
    }
}
