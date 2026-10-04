using System.Collections.Generic;
using UnityEngine;
using DragonArts.Common;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

namespace DragonArts.Collection.Countries {

    [Serializable]
    public class Country {
        public string name;
        public string isoCode2;
        public string isoCode3;
        public string capitalCity;
        public string dialingCode;
        private Sprite flag = null;
        private AudioClip anthem = null;

        public Country () {}

        public async Task<Sprite> LoadFlag (Action<Sprite> onLoadDone = null, Action onLoadFailed = null) {
            flag = await LoadFlag(isoCode3, onLoadDone, onLoadFailed);
            return flag;
        }

        public static async Task<Sprite> LoadFlag (string isoCode3, Action<Sprite> onLoadDone = null, Action onLoadFailed = null) {
            string address = $"DA/Collection/Countries/{isoCode3.ToUpper()}";
            return await LoadAsset<Sprite>(address, onLoadDone, onLoadFailed);
        }

        public void ReleaseFlag () {
            if (flag == null) return;
            Addressables.Release(flag);
            flag = null;
        }

        public async Task<AudioClip> LoadAnthem (Action<AudioClip> onLoadDone = null, Action onLoadFailed = null) {
            anthem = await LoadAnthem(isoCode3, onLoadDone, onLoadFailed);
            return anthem;
        }

        public static async Task<AudioClip> LoadAnthem (string isoCode3, Action<AudioClip> onLoadDone = null, Action onLoadFailed = null) {
            string address = $"DA/Collection/Countries/Anthems/{isoCode3.ToUpper()}";
            return await LoadAsset<AudioClip>(address, onLoadDone, onLoadFailed);
        }

        public void ReleaseAnthem () {
            if (anthem == null) return;
            Addressables.Release(anthem);
            anthem = null;
        }

        private async static Task<T> LoadAsset<T> (string address, Action<T> onLoadDone = null, Action onLoadFailed = null) where T: UnityEngine.Object {
            T asset = null;
            bool error = false;
            Addressables.LoadAssetAsync<T>(address).Completed += (AsyncOperationHandle<T> handle) => {
                if (handle.OperationException != null) {
                    error = true;
                    asset = null;
                } else {
                    error = false;
                    asset = handle.Result;
                }
            };

            while (asset == null && !error) {
                await Task.Yield();
            }

            if (error) onLoadFailed?.Invoke();
            else onLoadDone?.Invoke(asset);

            return asset;
        }
    }
}
