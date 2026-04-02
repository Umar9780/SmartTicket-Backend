using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartTicketSystemBackend.Services
{
    public class OpenAiService : IAiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _model;

        public OpenAiService(IConfiguration config)
        {
            _apiKey = config["Ai:OpenAI:ApiKey"]!;
            _model  = config["Ai:OpenAI:Model"] ?? "gpt-4o-mini";
            _http   = new HttpClient { BaseAddress = new Uri("https://api.openai.com") };
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> SummarizeTicketAsync(string subject, string description,
            string status, string priority, string? assignedTo, List<string> comments)
        {
            var commentBlock = comments.Count > 0
                ? "\n\nComments:\n" + string.Join("\n", comments.Select((c, i) => $"{i + 1}. {c}"))
                : "";

            var userMessage =
                $"Summarize this support ticket in 2-3 concise sentences covering the issue, " +
                $"current status, and any resolution steps mentioned.\n\n" +
                $"Subject: {subject}\n" +
                $"Status: {status} | Priority: {priority}" +
                (assignedTo != null ? $" | Assigned to: {assignedTo}" : "") +
                $"\n\nDescription:\n{description}" +
                commentBlock;

            var payload = new
            {
                model = _model,
                max_tokens = 250,
                messages = new[]
                {
                    new { role = "system", content = "You are a concise support ticket summarizer. Return plain text only, no markdown." },
                    new { role = "user",   content = userMessage }
                }
            };

            var response = await _http.PostAsync("/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Unable to generate summary.";
        }
    }
}
