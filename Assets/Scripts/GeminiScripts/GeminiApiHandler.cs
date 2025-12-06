using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class GeminiApiHandler : MonoBehaviour
{
    //*-----------------------------------------------------------------------------------------//
    #region Inspector Tab

    [Header("Gemini Ayarları -----------------------------------------------------------")]
    [Space]
    //[SerializeField] private Api_Key_SO Api_Key_SO;
    [SerializeField] private string model = "gemini-2.5-flash-lite";
    [SerializeField] public string ApiKey = "YOUR_GEMINI_API_KEY_HERE";
    [Space]
    [Header("Yanıt ----------------------------------------------------------------------")]
    [Space]
    [TextArea(3, 10)]
    public string LastResponse = "";
    [Space]
    public bool IsResponseReceived = false;
    public bool IsRequestInProgress = false;
    [Space]
    [Header("Test Metni -----------------------------------------------------------------")]
    [Space]
    [SerializeField] private KeyCode Test_Tusu = KeyCode.W;
    [TextArea(3, 10)]
    [Space]
    [SerializeField] private string Test_Prompt;

    #endregion
    //*-----------------------------------------------------------------------------------------//
    #region Unity Life Cycle
    void Update()
    {
        if (Input.GetKeyDown(Test_Tusu))
        {
            SendPrompt(Test_Prompt);
        }
    }
    #endregion
    //*-----------------------------------------------------------------------------------------//
    #region Send_Prompt Public Func

    public void SendPrompt(string prompt)
    {
        // API anahtarının ayarlanıp ayarlanmadığını kontrol edin
        if (string.IsNullOrEmpty(ApiKey) || ApiKey == "YOUR_GEMINI_API_KEY_HERE")
        {
          //  Api_Key = Api_Key_SO.Default_Key;
        }

        if (IsRequestInProgress)
        {
            Debug.LogWarning("Uyarı: Önceki istek tamamlanmadan yeni bir istek gönderilemez.");
            return;
        }

        // Durum değişkenlerini sıfırla ve isteği başlat
        IsResponseReceived = false;
        IsRequestInProgress = true;
        LastResponse = "";

        StartCoroutine(Send_Prompt_To_Gemini(prompt));
    }

    #endregion
    //*-----------------------------------------------------------------------------------------//
    #region Private Funcs

    private IEnumerator Send_Prompt_To_Gemini(string prompt)
    {
        // API URL'si
        string url = $"https://generativelanguage.googleapis.com/v1/models/{model}:generateContent?key={ApiKey}";

        // Gönderilecek JSON gövdesi (EscapeJson kullanılarak prompt güvenli hale getirildi)
        string jsonBody = "{\"contents\": [{\"parts\": [{\"text\": \"" + EscapeJson(prompt) + "\"}]}]}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log($"Gemini'ye istek gönderiliyor ({model})...");

        // İsteği gönder ve yanıtı bekle
        yield return request.SendWebRequest();

        string result;

        if (request.result != UnityWebRequest.Result.Success)
        {
            // Hata durumunda
            result = $"API Hatası: {request.error} \n Tekrar Deneyiniz.";
            Debug.LogError("Gemini API Hatası: " + request.error + "\nYanıt: " + request.downloadHandler.text);
        }
        else
        {
            // Başarılı yanıt durumunda
            string json = request.downloadHandler.text;
            result = ExtractTextFromGeminiResponse(json);
            Debug.Log("Gemini Yanıtı:\n" + result);
        }

        LastResponse = result;
        IsResponseReceived = true;
        IsRequestInProgress = false;
    }

    //*-----------------------------------------------------------------------------------------//

    private string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r");
    }

    //*-----------------------------------------------------------------------------------------//

    private string ExtractTextFromGeminiResponse(string json)
    {
        const string marker = "\"text\": \"";
        int start = json.IndexOf(marker);

        if (start == -1)
        {
            return $"Yanıt çözülemedi veya filtrelendi. Tam JSON: {json}";
        }

        start += marker.Length;

        int end = json.IndexOf("\"", start);

        if (end == -1) return "Yanıt çözülemedi.";

        string result = json.Substring(start, end - start);
        return result.Replace("\\n", "\n").Replace("\\\"", "\"");
    }

    #endregion
    //*-----------------------------------------------------------------------------------------//
}