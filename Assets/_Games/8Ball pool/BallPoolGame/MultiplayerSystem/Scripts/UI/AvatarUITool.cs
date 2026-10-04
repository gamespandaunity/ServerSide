using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NetworkManagement
{
    public class AvatarUITool : ProductUITool
    {
        [SerializeField] private RawImage image;
        [SerializeField] private Texture2D[] defaultAvatars;

        void OnEnable()
        {
            ProductAvatar productAvatar = ProductAvatar.FindObjectOfType<ProductAvatar>();
            if (productAvatar)
            {
                string avatarName = productAvatar.GetIconName();
                string avatarURL = productAvatar.GetIconURL();
               
                ProductLines_OnSetParameters(typeof(ProductAvatar), new object[]{ avatarName, avatarURL });

            }
            ProductAvatar.OnSetParameters += ProductLines_OnSetParameters;
        }

        void OnDisable()
        {
            ProductAvatar.OnSetParameters -= ProductLines_OnSetParameters;
        }
        void ProductLines_OnSetParameters (System.Type type, object[] parameters)
        {
            if (type == typeof(ProductAvatar))
            {
                string avatarName = parameters[0].ToString();
                string avatarURL = parameters[1].ToString();
                ConstantsData_M.LogInfo("avatarName " + avatarName);
                if (!string.IsNullOrEmpty(avatarName))
                {
                    image.texture = (Texture)FindAvatarImageByName(avatarName);
                }
                else if (!string.IsNullOrEmpty(avatarURL))
                {
                    StartCoroutine(SetAvatarImage(avatarURL));
                }
            }
        }
        public IEnumerator SetAvatarImage(string avatarURL)
        {
            if (!string.IsNullOrEmpty(avatarURL))
            {
                DownloadManager.DownloadParameters parameters = new DownloadManager.DownloadParameters(avatarURL, "Player Avatar", DownloadManager.DownloadType.DownloadOrLoadFromDisc);
                yield return DownloadManager.Download(parameters);
                if (parameters.texture)
                {
                    image.texture = (Texture)parameters.texture;
                }
            }
            yield return null;
        }

        public Texture2D FindAvatarImageByName(string avatarName)
        {
            foreach (var item in defaultAvatars)
            {
                if (item.name == avatarName)
                {
                    return item;
                }
            }
            return null;
        }
    }
}
