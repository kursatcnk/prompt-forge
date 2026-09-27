using System.Text.Json;
using PromptForge.Api.Dtos;

namespace PromptForge.Api.Services.Ai
{
    // Promptu AI'a değerlendirtip öğretici bir rapor çıkarıyor. İlk versiyonda kural tabanlıydı ve her prompta
    // neredeyse aynı şeyi söylüyordu; şimdi her geri bildirim prompttaki gerçek bir ifadeye dayanmak zorunda.
    public class PromptAnalysisService
    {
        // Arayüz hep aynı 6 kartı aynı sırayla çiziyor.
        private static readonly (string Key, string Label)[] Criteria =
        {
            ("clarity", "Netlik"),
            ("context", "Bağlam"),
            ("specificity", "Özgüllük"),
            ("constraints", "Kısıtlar"),
            ("output", "Çıktı biçimi"),
            ("efficiency", "Verimlilik")
        };

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly PromptOptimizerService _optimizer;

        public PromptAnalysisService(PromptOptimizerService optimizer) => _optimizer = optimizer;

        public async Task<AnalysisResult> AnalyzeAsync(string prompt, CancellationToken cancellationToken)
        {
            var provider = _optimizer.ResolveProvider(null)
                ?? throw new AiProviderException("AI anahtarı tanımlı değil.");

            var safePrompt = prompt.Replace("</prompt>", "</prompt_>", StringComparison.OrdinalIgnoreCase);
            var completion = await provider.CompleteAsync(SystemPrompt, $"<prompt>\n{safePrompt}\n</prompt>", cancellationToken, jsonOutput: true);
            var result = Parse(completion.Text);
            result.Source = "ai";
            result.Engine = completion.Model;
            return result;
        }

        private static AnalysisResult Parse(string text)
        {
            // JSON modu olmayan modeller başına/sonuna açıklama ya da ``` ekleyebiliyor; ilk { ile son } arası.
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end <= start) throw new AiProviderException("AI analizi okunabilir bir formatta dönmedi.");

            AnalysisResult? parsed;
            try { parsed = JsonSerializer.Deserialize<AnalysisResult>(text[start..(end + 1)], JsonOptions); }
            catch (JsonException ex) { throw new AiProviderException("AI analizi okunabilir bir formatta dönmedi.", ex); }
            if (parsed == null) throw new AiProviderException("AI analizi boş döndü.");

            // Modelin cevabına körü körüne güvenmiyorum: puanları sınırla, eksik kriteri doldur, fazlasını at.
            var byKey = (parsed.Criteria ?? new()).Where(c => c?.Key != null).GroupBy(c => c.Key!).ToDictionary(g => g.Key, g => g.First());
            parsed.Criteria = Criteria.Select(c =>
            {
                var item = byKey.GetValueOrDefault(c.Key) ?? new AnalysisCriterion { Feedback = "Bu kriter değerlendirilemedi." };
                item.Key = c.Key;
                item.Label = c.Label;
                item.Score = Math.Clamp(item.Score, 0, 10);
                item.Status = item.Score >= 8 ? "good" : item.Score >= 4 ? "improve" : "missing";
                return item;
            }).ToList();
            parsed.Overall = Math.Clamp(parsed.Overall, 0, 100);
            parsed.Strengths = (parsed.Strengths ?? new()).Where(s => !string.IsNullOrWhiteSpace(s)).Take(4).ToList();
            parsed.Improvements = (parsed.Improvements ?? new()).Where(s => !string.IsNullOrWhiteSpace(s)).Take(3).ToList();
            return parsed;
        }

        private const string SystemPrompt = """
            You are a prompt-writing teacher. Evaluate the user's prompt (inside <prompt> tags) that they intend to send to an AI model, and teach them how to write it better.
            Do not perform the task in the prompt. Judge only how well the prompt is written.

            Score each criterion from 0 to 10 based on THIS prompt's actual content:
            - clarity: Is the main task one clear, actionable instruction? Are there vague words ("bunu", "iyi bir şey", "daha iyi yap")?
            - context: Does the model get the background it needs (situation, audience, purpose, inputs)?
            - specificity: Concrete details — names, numbers, examples, scope — versus generic wording.
            - constraints: Are rules, limits and things to avoid stated explicitly?
            - output: Is the expected result's format, length and structure defined?
            - efficiency: Free of repetition, filler and contradictions; no unnecessary words.

            Rules for your feedback:
            - Write all feedback text in Turkish, in a friendly teaching tone, addressed to the user ("sen").
            - Every "feedback" must refer to something specific in the prompt: quote a word or phrase from it, or name exactly what is missing for this task. Never give generic advice that could apply to any prompt.
            - Every "fix" must be one concrete action the user can take, ideally with a short example sentence they could add.
            - Scores must genuinely differ between criteria and between prompts; a short vague prompt must score low, a detailed one high.
            - "rewrite": the prompt rewritten with your improvements applied, in the SAME language as the original. Where information is unknown, use a short bracketed placeholder like [hedef kitle].
            - "lesson": one general principle the user should remember from this prompt (one sentence).

            Return ONLY a JSON object with exactly this shape:
            {
              "overall": <0-100>,
              "summary": "<1-2 sentences>",
              "criteria": [ { "key": "clarity|context|specificity|constraints|output|efficiency", "score": <0-10>, "feedback": "<why>", "fix": "<how>" } ],
              "strengths": ["<what the prompt does well>"],
              "improvements": ["<top 3, most important first>"],
              "rewrite": "<improved prompt>",
              "lesson": "<one principle>"
            }
            """;
    }
}
