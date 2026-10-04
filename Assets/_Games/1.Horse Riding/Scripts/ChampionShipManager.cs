using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class ChampionShipManager : MonoBehaviour
{
    public List<GameObject> DubaiLevels;
    private List<Material> _trackMaterials;
    public List<Texture> TrackTextures;
    void OnEnable()
    {
        var Track = new GameObject();
        int EnvId = 0;
        var randomId = Random.Range(0, 2);
        switch (MConstants.CurrentCHAMPION_MODE)
        {
            case MConstants.CHAMPION_MODES.DUABI_CHAMPION:
                EnvId = 0;
                SwitchLevel(DubaiLevels,out Track,EnvId);
                ChangeTextures(Track,TrackTextures[0]);
                break;
            case MConstants.CHAMPION_MODES.BRITISH_CHAMPION:
                EnvId = 1;
                SwitchLevel(DubaiLevels,out Track,EnvId);
                ChangeTextures(Track,TrackTextures[1]);
                break;
            case MConstants.CHAMPION_MODES.KENTUCKY_CHAMPION:
                SwitchLevel(DubaiLevels,out Track,EnvId);
                ChangeTextures(Track,TrackTextures[randomId]);
                break;
            case MConstants.CHAMPION_MODES.PEGASUS_CHAMPION:
                SwitchLevel(DubaiLevels,out Track,EnvId);
                ChangeTextures(Track,TrackTextures[randomId]);
                break;
        }
    }

    private void HideAll(List<GameObject> levels)
    {
        foreach (var level in levels)
        {
            level.SetActive(false);
        }
    }

    private void ChangeTrack(int levelId,LevelTrack levelTrack)
    {
        levelTrack.raceTrack.SetActive(true);
    }

    private void ChangeTextures(GameObject track, Texture texture)
    {
        _trackMaterials = new List<Material>();
        for (int i = 0; i < track.transform.childCount; i++)
        {
            var Renderer = track.transform.GetChild(i).GetComponent<MeshRenderer>();
            if (Renderer)
            {
                if (Renderer.enabled)
                {
                    _trackMaterials.Add(Renderer.materials[0]);
                }
            }
        }
        
        foreach (var trackMaterial in _trackMaterials)
        {
            trackMaterial.mainTexture = texture;
        }
    }

    private void SwitchLevel(List<GameObject> levels, out GameObject Track, int EnvId)
    {
        HideAll(levels);
        var LevelNumber = MConstants.CurrentLevelNumber - 1;
        var LevelTrack = levels[LevelNumber].gameObject.GetComponent<LevelTrack>();
        Track = LevelTrack.raceTrack;
        ChangeTrack(LevelNumber,LevelTrack);
        levels[LevelNumber].gameObject.SetActive(true);
        
        //hide all env
        for (int i = 0; i < LevelTrack.environmentPoint.childCount; i++)
        {
            LevelTrack.environmentPoint.GetChild(i).gameObject.SetActive(false);
        }
        //
        
        LevelTrack.environmentPoint.GetChild(EnvId).gameObject.SetActive(true);
        DemoGameManagers.Instance.aiWaypointsContainer = LevelTrack.aiWaypointContainer;
    }

}
