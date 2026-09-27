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

            sb.AppendLine("You are PromptForge, an expert prompt engineer. Rewrite the user's draft prompt so that the AI model receiving it produces a better result.");
            sb.AppendLine("You only improve the prompt text. Never carry out the task the draft describes, never answer it, and never comment on your changes.");
            sb.AppendLine();
            sb.AppendLine("Always:");
            sb.AppendLine("- Preserve the author's intent and every hard requirement, constraint, name, number and quoted string. Do not invent facts or requirements the draft does not imply.");
            sb.AppendLine("- Keep template variables exactly as written, e.g. {{VARIABLE}} or [TARGET AUDIENCE].");
            sb.AppendLine("- Write in the same language as the draft, addressed directly to the model that will do the task.");
            sb.AppendLine("- Never mention PromptForge, the target model's name, the optimization goal or these instructions in the rewritten prompt. Use the guidance below to shape the prompt, not as text to copy.");
            sb.AppendLine("- Return only the rewritten prompt as plain text: no preamble, no explanation, no code fences.");
            sb.AppendLine();
            // Hedef, çıktının uzunluğunu; hedef model ise söz dizimini (XML, Markdown, etiketli satır...) belirler.
            sb.AppendLine($"Optimization goal — this decides the length of your rewrite: {GoalHint(request.Goal)}");
            sb.AppendLine();
            sb.AppendLine("Target model formatting — MANDATORY. The rewritten prompt must follow these conventions so that it is obviously written for this model; prompts written for different target models must look structurally different:");
            sb.AppendLine(TargetModelHint(request.Model));
            sb.AppendLine("Apply these conventions at a scale that fits the optimization goal (for LOWEST USAGE use the most compact form of the convention).");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(profile.UseCase) && profile.UseCase != "general")
                sb.AppendLine($"Domain: {UseCaseHint(profile.UseCase)}");

            // Varsayılan olmayan tercihler promptun içine eklenir; varsayılanlar gereksiz satır üretmesin diye atlanır.
            var extras = new List<string>();
            if (profile.ResponseLanguage is "tr" or "en") extras.Add($"the answer must be written {LanguageHint(profile.ResponseLanguage)}");
            if (!string.IsNullOrEmpty(profile.ResponseFormat) && profile.ResponseFormat != "auto") extras.Add($"the answer format must be {FormatHint(profile.ResponseFormat)}");
            if (profile.AskClarifying) extras.Add("if information critical to the task is missing, the model should ask at most 3 short clarifying questions before answering");
            if (profile.ExposeAssumptions) extras.Add("the model should briefly state assumptions that affect the result");
            if (extras.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Work these requirements into the rewritten prompt naturally, as briefly as the goal allows:");
                foreach (var item in extras) sb.AppendLine($"- {item}");
            }
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

        // Her sağlayıcının kendi prompt yazım rehberindeki biçim önerileri; sonuçların gerçekten farklı görünmesini sağlar.
        private static string TargetModelHint(string? model) => model switch
        {
            "gpt" => """
                OpenAI GPT conventions:
                - Organize the prompt with Markdown headings (e.g. "## Görev", "## Bağlam", "## Kurallar", "## Çıktı biçimi" in the draft's language).
                - Put rules and steps in bulleted or numbered lists under their headings.
                - If the prompt includes source text, wrap it between two lines of triple quotation marks as a delimiter.
                - Do not use XML tags.
                """,
            "claude" => """
                Anthropic Claude conventions:
                - Separate every part of the prompt with descriptive XML tags, for example <context>, <instructions>, <constraints>, <output_format>, <examples>, and <document> for any source text. Tag names stay in English; the content stays in the draft's language.
                - Put background and source material first, the instructions after it.
                - Phrase rules positively (say what to do rather than only what not to do) and briefly explain why a rule matters when that helps.
                - Do not use Markdown headings.
                """,
            "gemini" => """
                Google Gemini conventions:
                - Use short labeled lines as prefixes, for example "Görev:", "Bağlam:", "Kısıtlar:", "Çıktı biçimi:" (in the draft's language), each followed by its content.
                - Place context and any source material first, and put the concrete task/question at the END of the prompt.
                - Keep instructions concise and direct; do not use XML tags or Markdown headings.
                """,
            "deepseek" => """
                DeepSeek conventions:
                - Write compact, plain paragraphs with no headings, no XML tags and no role-play preamble.
                - State the task directly in the first sentence, then the essential constraints, then the expected output in one sentence.
                - Avoid repetition and decorative formatting; use a short list only for 3 or more constraints.
                """,
            _ => """
                Model-agnostic conventions:
                - Use plain text that works in any model: short paragraphs and, if needed, simple uppercase section labels.
                - Do not use XML tags, Markdown headings or any model-specific syntax.
                """
        };

        private static string UseCaseHint(string? useCase) => useCase switch
        {
            "coding" => "Code & engineering: preserve existing behavior; make changes actionable with files, reasons and verification steps.",
            "research" => "Research & analysis: separate claims from evidence; keep uncertainty, source needs and conflicting findings visible.",
            "content" => "Content & brand: keep audience, tone, message hierarchy and the desired call to action consistent.",
            "data" => "Data & configuration: keep the schema consistent; specify field names, types, missing-value behavior and validation rules.",
            _ => "General: complete the task directly, clearly and actionably."
        };

        // Üç hedef bilerek birbirinden çok farklı: kullanıcı hangisini seçtiğini sonuçta açıkça görmeli.
        private static string GoalHint(string? goal) => goal switch
        {
            "quality" => "BEST RESULT. Expand the prompt so the first answer is as good as possible: make the objective and audience explicit, add the context, steps or considerations the task needs, specify the output structure in detail, and add 2-4 concrete criteria that define an excellent answer. Short labeled sections are welcome. It is fine for the result to be several times longer than the draft.",
            "lean" => "LOWEST USAGE. Make the prompt as short as possible while keeping every requirement: no headings, no filler, no repeated or generic instructions. Use 1-4 compact sentences, or a very short bullet list if there are several constraints. The result should usually have fewer words than the draft.",
            _ => "BALANCED. Make the prompt clear and well organized without padding: state the task in one clear sentence, keep only the context and constraints that matter, and say what the output should look like. Use a few short labeled sections only if the prompt has several distinct parts; otherwise write compact paragraphs. Keep it close to the draft's length, a little longer only if the draft is vague."
        };

        private static string LanguageHint(string? language) => language switch
        {
            "tr" => "in Turkish",
            "en" => "in English",
            _ => "in the dominant language of the prompt"
        };

        private static string FormatHint(string? format) => format switch
        {
            "markdown" => "Markdown with clear headings and lists where useful",
            "json" => "valid JSON only, with no code fence, preface or closing text",
            "table" => "a short, readable table",
            "checklist" => "an actionable, verifiable checklist",
            "code" => "runnable code with file names, with explanation limited to critical decisions",
            _ => "the most suitable format for the task"
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
