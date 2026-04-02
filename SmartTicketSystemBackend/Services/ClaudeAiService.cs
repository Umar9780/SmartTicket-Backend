using System.Text;
using System.Text.Json;

namespace SmartTicketSystemBackend.Services
{
    // Ready to use when you switch to Claude — just change Ai:Provider to "Claude" in appsettings.json
    public class ClaudeAiService : IAiService
    {
        private readonly HttpClient _http;
        private readonly string _model;

        public ClaudeAiService(IConfiguration config)
        {
            var apiKey = config["Ai:Claude:ApiKey"]!;
            _model     = config["Ai:Claude:Model"] ?? "claude-haiku-4-5-20251001";
            _http      = new HttpClient { BaseAddress = new Uri("https://api.anthropic.com") };
            _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
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
                system = "You are a concise support ticket summarizer. Return plain text only, no markdown.",
                messages = new[]
                {
                    new { role = "user", content = userMessage }
                }
            };

            var response = await _http.PostAsync("/v1/messages",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "Unable to generate summary.";
        }
    }
}
