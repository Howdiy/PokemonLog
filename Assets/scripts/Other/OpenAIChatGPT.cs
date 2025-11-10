using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json; // Newtonsoft.Json 사용

public class OpenAIChatGPT : MonoBehaviour
{   // 실제 API 키로 대체
    private string apiKey = "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\r\n";
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    public IEnumerator GetChatGPTResponse(string prompt, System.Action<string> callback)
    {
        // OpenAI API 요청 데이터 설정
        var jsonData = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 20
        };

        string jsonString = JsonConvert.SerializeObject(jsonData);

        // HTTP 요청 설정
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
        }
        else
        {
            var responseText = request.downloadHandler.text;
            Debug.Log("Response: " + responseText);
            // JSON 응답을 분석하여 필요한 부품을 추출합니다
            var response = JsonConvert.DeserializeObject<OpenAIResponse>(responseText);
            callback(response.choices[0].message.content.Trim());
        }
    }

    public class OpenAIResponse
    {
        public Choice[] choices { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}
