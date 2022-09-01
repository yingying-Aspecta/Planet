using System.IO;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System.Collections;
using UnityEngine.Events;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public List<Planet> planetList;
    public Planet currentPlanet;
    public UnityEvent<EGameState> onStateChange;
    public GameObject planetPref;
    private EGameState currentState;

    public void Awake() 
    {
        Load();
        instance = this;
    }

    public void Start()
    {
        SetState(EGameState.GlobalView,Sun.instance);
    }

    /// <summary>
    /// 判断游戏状态
    /// </summary>
    public bool CompareState(EGameState state)
    {
        return currentState == state;
    }

    /// <summary>
    /// 设定状态机
    /// </summary>
    [Button("SetState")]
    public void SetState(EGameState state,Planet planet)
    {
        onStateChange?.Invoke(state);
        switch (state)
        {
            case EGameState.GlobalView:
                CameraControl.instance.SetTarget(new CameraData(Sun.instance.transform,-24));
                break;
            case EGameState.PlanetView:
                planet.CameraFollow();
                break;
            case EGameState.Editor:
                planet.CameraFollow();
                break;
            default:
                break;
        }
        currentState = state;
        currentPlanet = planet;
    }

    /// <summary>
    /// 编辑星球
    /// </summary>
    public void EditPlanet(Land land,Vector3 activeDir)
    {
        switch (UIManager.Instance.editorPanel.currentState)
        {
            case EditorState.add:
                if (!land.planet.data.InRange(land.GetPos(activeDir)))
                    break;
                var newLand=LandPool.Instance.GetLand(land.GetPos(activeDir), land.planet);
                newLand.planet.data.SetLandKind(land, "surface");
                newLand.planet.data.Update(land);
                break;
            case EditorState.remove:
                land.planet.data.SetLandKind(land, "empty");
                land.planet.data.Update(land);
                LandPool.Instance.DestoryLand(land);
                break;
            default :
                break;
        }
    }

    [Button("Save")]
    public void Save()
    {
        using (FileStream file = new FileStream("planet2.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
        {
            JObject output = new JObject();
            BinaryFormatter formatter = new BinaryFormatter();
            foreach (var item in planetList)
            {
                output[item.name] = item.data.Serialize();
            }
            formatter.Serialize(file, output.ToString());
        }
        Debug.Log("Save Success");
    }

    [Button("Load")]
    public void Load()
    {
        using (FileStream file = new FileStream("planet2.json", FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            JObject output = JObject.Parse(formatter.Deserialize(file) as string);
            foreach (var item in output)
            {
                var planet = Instantiate(planetPref).GetComponent<Planet>();
                planet.name = item.Key;
                //planet.data= new PlanetData();
                StartCoroutine(planet.data.Deserialize(item.Value as JObject));
                planet.transform.position = Sun.instance.transform.position + Vector3.right * planet.data.trackRadius;
            }
        }
        Debug.Log("Load Success");
    }
}

/// <summary>
/// 游戏状态枚举
/// </summary>
public enum EGameState
{
    GlobalView,
    PlanetView,
    Editor
}