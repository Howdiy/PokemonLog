using UnityEngine;

public class ChatGPTExample : MonoBehaviour
{
    [SerializeField, TextArea(3, 5)] private string prompt;

    void Start()
    {
        OpenAIChatGPT chatGPT = gameObject.AddComponent<OpenAIChatGPT>();
        StartCoroutine(chatGPT.GetChatGPTResponse(prompt, OnResponseReceived));
    }

    void OnResponseReceived(string response)
    {
        Debug.Log("ChatGPT Response: " + response);
    }
}


