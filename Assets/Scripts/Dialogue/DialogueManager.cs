using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public GameManager gameManager;

    private void Start()
    {
        DisplayDialogue("Clara", "player_intro_to_clara");
    }

    public DialogueNode GetDialogue(string characterName, string id)
    {
        string path = Application.dataPath + "/Scripts/Dialogue/Characters/" + characterName + ".json";
        string json = File.ReadAllText(path);

        DialogueData dialogueData = JsonConvert.DeserializeObject<DialogueData>(json);
        DialogueNode node = dialogueData.dialogue[id];
        return node;
    }
    
    public void DisplayDialogue(string characterName, string id)
    {
        DialogueNode node = GetDialogue(characterName, id);
        Debug.Log("Speaker: " + node.speaker + " Text: " + gameManager.selectedLanguage.text[node.id]);
    }
}