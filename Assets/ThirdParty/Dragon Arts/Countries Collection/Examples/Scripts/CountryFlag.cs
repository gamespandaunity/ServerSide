using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragonArts.Collection.Countries;
using UnityEngine.AddressableAssets;
using System;
using TMPro;

namespace DragonArts.Collection.Countries.Example {

    public class CountryFlag : MonoBehaviour { // a Country Flag
        
        [SerializeField] private string countryCode; // ISO Code (3)
        [SerializeField] private GameObject flagGameObject;
        private Renderer flagRenderer;
        private Sprite flagSprite;

        private async void Awake () {
            flagRenderer = flagGameObject.GetComponent<Renderer>();
            flagSprite = await Country.LoadFlag(countryCode); // Load Country Flag (Addressable) by Country ISO Code (3) - Static Method
            flagRenderer.material.mainTexture = ConvertSpriteToTexture(flagSprite); // Convert Sprite to Texture
        }

        private void OnDestroy () {
            // If you Load Country Flag with Static Method, then you have to handle releasing by yourself
            Addressables.Release(flagSprite);
        }

        private Texture ConvertSpriteToTexture (Sprite sprite) {
            var croppedTexture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
            var pixels = sprite.texture.GetPixels((int)sprite.textureRect.x, (int)sprite.textureRect.y, (int)sprite.textureRect.width, (int)sprite.textureRect.height);
            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply();
            return croppedTexture;
        }
    }
}
