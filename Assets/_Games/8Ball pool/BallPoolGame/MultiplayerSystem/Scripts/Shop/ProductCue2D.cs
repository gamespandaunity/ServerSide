using System.Collections;
using UnityEngine;

namespace NetworkManagement
{
    public class ProductCue2D : NetworkManagement.ProductWithTexturs2D
    {
        public Texture cueDefault2DTexture{ get; private set; }
        public Texture mainCue2DTexture{ get; private set; }
        public Material cue2dMaterial;

        void Start()
        {
            cueDefault2DTexture = cue2dMaterial.mainTexture;
        }
        protected override IEnumerator SetSources()
        {
            yield return StartCoroutine(base.SetSources());
          
            mainCue2DTexture = materials[0].mainTexture;
        }
    }
}