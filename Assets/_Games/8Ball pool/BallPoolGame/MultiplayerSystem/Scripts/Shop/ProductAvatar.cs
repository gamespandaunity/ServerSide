using System.Collections;
using UnityEngine;

namespace NetworkManagement
{
    public class ProductAvatar : NetworkManagement.Product
    {
        public string avatarURL{ get; private set; }
        public string avatarName{ get; private set; }

       

        protected override IEnumerator FirstInitializeProduct()
        {
            yield return StartCoroutine(base.FirstInitializeProduct());
        }
        public override IEnumerator InitializeProduct(ProductProfile productProfile)
        {
            yield return StartCoroutine(base.InitializeProduct(productProfile));
            yield return StartCoroutine(SetSources());
        }

        protected override IEnumerator SetSources()
        {
            if (this.productProfile != null)
            {
                if (!string.IsNullOrEmpty(this.productProfile.data.iconURL))
                {
                    iconeURL = this.productProfile.data.iconURL;
                    avatarName = "";
                }
                else
                {
                    iconeURL = "";
                    avatarName = productProfile.data.name;
                }
            }
            else
            {
                avatarURL = GetIconURL();
                avatarName = GetIconName();
            }
            avatarURL = iconeURL;

            SaveIconURL(iconeURL);
            SaveIconName(avatarName);

            if (!string.IsNullOrEmpty(avatarURL) || !string.IsNullOrEmpty(avatarName))
            {
            }
           
            yield return null;
        }

       


        public override void ResetWhenBackToEditor()
        {

        }
       
    }
}
