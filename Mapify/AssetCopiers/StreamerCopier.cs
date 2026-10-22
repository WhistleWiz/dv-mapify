using System.Collections.Generic;
using System.Linq;
using Mapify.Editor;
using Mapify.Editor.Utils;
using UnityEngine;

namespace Mapify.SceneInitializers.Vanilla.Streaming
{
    public class StreamerCopier : AssetCopier
    {
        protected override IEnumerator<(VanillaAsset, GameObject)> ToSave(GameObject gameObject)
        {
            foreach (MeshFilter filter in gameObject.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null) continue;
                switch (mesh.name)
                {
                    case "TurntablePit": {
                        Transform parent = Object.Instantiate(filter.transform.parent.gameObject).transform;
                        parent.gameObject.SetActive(false);
                        Object.Destroy(parent.FindChildByName("TurntableControlHouse").gameObject);
                        Object.Destroy(parent.FindChildByName("TurntableControlHouse_LOD1").gameObject);
                        Object.Destroy(parent.FindChildByName("TurntableControlHouse_ShadowCaster").gameObject);
                        foreach (Transform t in parent)
                            t.localPosition = Vector3.zero;
                        yield return (VanillaAsset.TurntablePit, parent.gameObject);
                        break;
                    }
                    case "TurntableControlHouse":
                    {
                        var shedVisual = filter.transform.parent.gameObject;
                        var shedCopy = Object.Instantiate(shedVisual);
                        shedCopy.SetActive(false);
                        Object.Destroy(shedCopy.FindChildByName("TurntablePit"));
                        Object.Destroy(shedCopy.FindChildByName("TurntablePit_LOD1"));
                        Object.Destroy(shedCopy.FindChildByName("TurntablePit_ShadowCaster"));

                        var shedCollider = shedVisual.transform.parent.GetComponentsInChildren<MeshCollider>()
                            .First(collider => collider.sharedMesh == mesh);
                        Object.Instantiate(shedCollider.gameObject, shedCopy.transform, false);

                        foreach (Transform t in shedCopy.transform)
                            t.localPosition = Vector3.zero;

                        yield return (VanillaAsset.TurntableControlShed, shedCopy);
                        break;
                    }
                    case "ItemShop":
                        yield return (VanillaAsset.StoreMesh, filter.transform.parent.gameObject);
                        break;
                }
            }

            foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material == null) continue;
                switch (material.name)
                {
                    case "BallastLOD":
                        yield return (VanillaAsset.BallastLodMaterial, renderer.gameObject);
                        break;
                }
            }
        }
    }
}
