using Newtonsoft.Json;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Language selectedLanguage = null;

    public void Awake()
    {
        string path = Application.dataPath + "/Scripts/Dialogue/Languages/en.json";
        string json = File.ReadAllText(path);

        selectedLanguage = JsonConvert.DeserializeObject<Language>(json);
    }
}