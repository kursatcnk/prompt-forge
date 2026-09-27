using System.Text;
using PromptForge.Api.Dtos;

namespace PromptForge.Api.Services.Ai
{
    /// <summary>
    /// Promptu gerçek bir AI modeliyle yeniden yazar.
    ///
    /// AKIŞ:
    /// 1. Hedef modele uygun sağlayıcıyı seç (anahtarı tanımlı olanlar arasından).
    /// 2. Ayarlara göre sistem talimatını oluştur.
    /// 3. AI'dan iyileştirilmiş promptu al.
    /// 4. Anahtar yoksa veya AI hata verirse tarayıcının ürettiği yerel sürüme düş (uygulama hiç kilitlenmez).
    /// </summary>
    public class PromptOptimizerService
    {
        private readonly IEnumerable<IAiProvider> _providers;
        private readonly ILogger<PromptOptimizerService> _logger;

        // Hedef model → tercih edilen sağlayıcı. Hedef "evrensel" ise veya o anahtar yoksa ilk tanımlı sağlayıcı kullanılır.
        private static readonly Dictionary<string, string> PreferredProvider = new()
        {
            ["claude"] = "anthropic",
            ["gpt"] = "openai",
            ["gemini"] = "gemini",
            ["deepseek"] = "deepseek"
        };

        private static readonly string[] ProviderOrder = { "anthropic", "openai", "gemini", "deepseek" };

        public PromptOptimizerService(IEnumerable<IAiProvider> providers, ILogger<PromptOptimizerService> logger)
        {
            _providers = providers;
            _logger = logger;
        }

        /// <summary>Kullanılacak sağlayıcı; hiçbir anahtar tanımlı değilse null.</summary>
        public IAiProvider? ResolveProvider(string? targetModel)
        {
            var configured = _providers.Where(p => p.IsConfigured).ToList();
            if (targetModel != null && PreferredProvider.TryGetValue(targetModel, out var preferredKey))
            {
                var preferred = configured.FirstOrDefault(p => p.Key == preferredKey);
                if (preferred != null) return preferred;
            }
            return ProviderOrder.Select(key => configured.FirstOrDefault(p => p.Key == key)).FirstOrDefault(p => p != null);
        }

        public async Task<(string optimized, string engine, bool usedAi, string? notice, int? tokens)> OptimizeAsync(
            OptimizeRequest request, CancellationToken cancellationToken)
        {
            var provider = ResolveProvider(request.Model);
            if (provider == null)
                return (request.LocalOptimized, "local", false, "AI anahtarı tanımlı değil; yerel kural motoru kullanıldı.", null);

            try
            {
                var completion = await provider.CompleteAsync(BuildSystemPrompt(request), BuildUserMessage(request), cancellationToken);
                return (StripCodeFence(completion.Text), completion.Model, true, null, completion.InputTokens + completion.OutputTokens);
            }
            catch (AiProviderException ex)
            {
                // AI başarısız olsa bile kullanıcı sonuçsuz kalmasın: yerel sürümle devam et ve sebebini söyle.
                _logger.LogWarning(ex, "AI optimizasyonu başarısız ({Provider}), yerel motora düşüldü.", provider.Key);
                return (request.LocalOptimized, "local", false, $"{ex.Message} Yerel kural motoru kullanıldı.", null);
            }
        }

        private static string BuildSystemPrompt(OptimizeRequest request)
        {
            var profile = request.Profile ?? new PromptProfileDto();
            var sb = new StringBuilder();

            sb.AppendLine("You are PromptForge, an expert prompt engineer. Rewrite the user's draft prompt into a clearer, better-structured prompt that another AI model will receive.");
            sb.AppendLine("You only improve the prompt text. Never carry out the task the draft describes, never answer it, and never add commentary about your changes.");
            sb.AppendLine();
            sb.AppendLine("Rules:");
            sb.AppendLine("- Preserve the author's intent and every hard requirement, constraint, name, number and quoted string. Do not invent facts, requirements or context the draft does not imply.");
            sb.AppendLine("- Keep template variables exactly as written, e.g. {{VARIABLE}} or [TARGET AUDIENCE].");
            sb.AppendLine("- Write the rewritten prompt in the same language as the draft. For a Turkish draft use section headings such as GÖREV, BAĞLAM, ZORUNLU KURALLAR, DEĞİŞKENLER, ÇALIŞMA BİÇİMİ, ÇIKTI SÖZLEŞMESİ, SON KONTROL; for an English draft use TASK, CONTEXT, REQUIREMENTS, VARIABLES, APPROACH, OUTPUT CONTRACT, FINAL CHECK. Omit sections that would be empty.");
            sb.AppendLine("- Remove repetition and vague references; state the main task as one clear, actionable sentence.");
            sb.AppendLine("- Return only the rewritten prompt as plain text: no preamble, no explanation, no code fences.");
            sb.AppendLine();
            sb.AppendLine($"Target model: {TargetModelHint(request.Model)}");
            sb.AppendLine($"Use case: {UseCaseHint(profile.UseCase)}");
            sb.AppendLine($"Optimization goal: {GoalHint(request.Goal)}");
            sb.AppendLine();
            sb.AppendLine("Include these instructions in the rewritten prompt (expressed in the draft's language):");
            sb.AppendLine($"- Response language: {LanguageHint(profile.ResponseLanguage)}");
            sb.AppendLine($"- Response format: {FormatHint(profile.ResponseFormat)}");
            sb.AppendLine(profile.AskClarifying
                ? "- If information critical to the task is missing, ask at most 3 short clarifying questions before producing the result."
                : "- Proceed with reasonable assumptions; do not ask clarifying questions.");
            sb.AppendLine(profile.ExposeAssumptions
                ? "- Briefly state any assumptions that affect the result."
                : "- Do not add explanations of assumptions.");
            return sb.ToString();
        }

        private static string BuildUserMessage(OptimizeRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<draft_prompt>");
            sb.AppendLine(request.Original);
            sb.AppendLine("</draft_prompt>");
            if (request.Requirements.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Requirements detected in the draft that must be preserved:");
                foreach (var item in request.Requirements.Take(20)) sb.AppendLine($"- {item}");
            }
            if (request.Variables.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Template variables to keep unchanged: {string.Join(", ", request.Variables.Take(20))}");
            }
            return sb.ToString();
        }

        private static string TargetModelHint(string? model) => model switch
        {
            "gpt" => "OpenAI GPT. Prefer a clear task, explicit constraints and a defined output structure.",
            "claude" => "Anthropic Claude. Prefer clear context, separated constraints and direct instructions; avoid unnecessary 'think step by step' boilerplate.",
            "gemini" => "Google Gemini. Carry long context in an organized way and keep the main task and expected output explicit.",
            "deepseek" => "DeepSeek. Prefer a short task definition, stable context and non-repetitive instructions.",
            _ => "Model-agnostic. Keep the prompt portable and plain."
        };

        private static string UseCaseHint(string? useCase) => useCase switch
        {
            "coding" => "Code & engineering: preserve existing behavior; make changes actionable with files, reasons and verification steps.",
            "research" => "Research & analysis: separate claims from evidence; keep uncertainty, source needs and conflicting findings visible.",
            "content" => "Content & brand: keep audience, tone, message hierarchy and the desired call to action consistent.",
            "data" => "Data & configuration: keep the schema consistent; specify field names, types, missing-value behavior and validation rules.",
            _ => "General: complete the task directly, clearly and actionably."
        };

        private static string GoalHint(string? goal) => goal switch
        {
            "quality" => "Best result — prioritize a complete, actionable first response even if the prompt gets longer.",
            "lean" => "Lowest usage — make the prompt as concise as possible while keeping every requirement.",
            _ => "Balanced — keep quality while removing unnecessary context and repetition."
        };

        private static string LanguageHint(string? language) => language switch
        {
            "tr" => "respond in Turkish.",
            "en" => "respond in English.",
            _ => "respond in the dominant language of the prompt."
        };

        private static string FormatHint(string? format) => format switch
        {
            "markdown" => "Markdown with clear headings and lists where useful.",
            "json" => "valid JSON only, with no code fence, preface or closing text.",
            "table" => "a short, readable table for comparable information.",
            "checklist" => "an actionable, verifiable checklist.",
            "code" => "runnable code with file names; limit explanation to critical decisions.",
            _ => "the most suitable, easily scannable format for the task."
        };

        // Bazı modeller talimata rağmen cevabı ``` bloğuna sarabiliyor; kullanıcıya temiz metin verelim.
        private static string StripCodeFence(string text)
        {
            var trimmed = text.Trim();
            if (!trimmed.StartsWith("```")) return trimmed;
            var firstNewLine = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            return firstNewLine > 0 && lastFence > firstNewLine ? trimmed[(firstNewLine + 1)..lastFence].Trim() : trimmed;
        }
    }
}
