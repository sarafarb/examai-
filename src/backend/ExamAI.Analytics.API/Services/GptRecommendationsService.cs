using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExamAI.Analytics.API.Models;

namespace ExamAI.Analytics.API.Services
{
    public class GptRecommendationsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey = "YOUR_API_KEY"; // אמור להגיע מ-IConfiguration

        public GptRecommendationsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetRecommendationsAsync(ExamAnalytics analyticsData)
        {
            var prompt = $@"
                אתה יועץ פדגוגי מומחה. נתח את הנתונים הבאים של מבחן וכתוב 3 המלצות פעולה קצרות למורה בעברית.
                נתונים:
                ממוצע כיתה: {analyticsData.Average}
                אחוז עוברים: {analyticsData.PassRate}%
                ציון מינימלי: {analyticsData.MinScore}
                ציון מקסימלי: {analyticsData.MaxScore}
                סטיית תקן: {analyticsData.StandardDeviation} (סטיית תקן גבוהה מעידה על פערים גדולים בכיתה).
                
                החזר את התשובה כרשימה של נקודות (Bullet points) בלבד, ללא הקדמות.
            ";

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 300,
                temperature = 0.7
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }
    }
}