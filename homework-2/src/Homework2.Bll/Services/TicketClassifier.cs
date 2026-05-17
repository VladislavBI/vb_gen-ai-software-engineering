using Homework2.Bll.Domain;

namespace Homework2.Bll.Services;

/// <summary>Service for keyword-based ticket classification.</summary>
public class TicketClassifier
{
    private static readonly Dictionary<Priority, IReadOnlyList<string>> PriorityKeywords = new()
    {
        { Priority.Urgent, new[] { "can't access", "critical", "production down", "security" } },
        { Priority.High, new[] { "important", "blocking", "asap" } },
        { Priority.Low, new[] { "minor", "cosmetic", "suggestion" } }
    };

    private static readonly Dictionary<Category, IReadOnlyList<string>> CategoryKeywords = new()
    {
        { Category.AccountAccess, new[] { "login", "password", "2fa", "access", "account" } },
        { Category.TechnicalIssue, new[] { "bug", "error", "crash", "exception", "fail" } },
        { Category.BillingQuestion, new[] { "payment", "invoice", "refund", "billing", "charge" } },
        { Category.FeatureRequest, new[] { "feature", "enhancement", "improve", "request", "wish" } },
        { Category.BugReport, new[] { "defect", "reproduce", "steps to reproduce", "regression" } }
    };

    /// <summary>Classifies a ticket based on keyword matching.</summary>
    /// <param name="ticket">The ticket to classify.</param>
    /// <returns>A classification result containing category, priority, confidence, reasoning, and matched keywords.</returns>
    public ClassificationResult Classify(Ticket ticket)
    {
        string searchText = ticket.Subject + " " + ticket.Description;

        Priority priority = ClassifyPriority(searchText);
        (Category category, double categoryConfidence) = ClassifyCategory(searchText);
        List<string> matchedKeywords = FindMatchedKeywords(searchText, categoryConfidence == 0 ? Category.Other : category);

        string reasoning = GenerateReasoning(category, priority, categoryConfidence, matchedKeywords);

        return new ClassificationResult(
            Category: category,
            Priority: priority,
            Confidence: categoryConfidence,
            Reasoning: reasoning,
            KeywordsFound: matchedKeywords
        );
    }

    private static Priority ClassifyPriority(string searchText)
    {
        if (MatchesAnyKeyword(searchText, PriorityKeywords[Priority.Urgent]))
        {
            return Priority.Urgent;
        }

        if (MatchesAnyKeyword(searchText, PriorityKeywords[Priority.High]))
        {
            return Priority.High;
        }

        if (MatchesAnyKeyword(searchText, PriorityKeywords[Priority.Low]))
        {
            return Priority.Low;
        }

        return Priority.Medium;
    }

    private static (Category, double) ClassifyCategory(string searchText)
    {
        Category bestCategory = Category.Other;
        double bestConfidence = 0.0;

        foreach (KeyValuePair<Category, IReadOnlyList<string>> kvp in CategoryKeywords)
        {
            int matched = CountMatchedKeywords(searchText, kvp.Value);
            if (matched == 0)
            {
                continue;
            }

            double confidence = (double)matched / kvp.Value.Count;
            if (confidence > bestConfidence)
            {
                bestConfidence = confidence;
                bestCategory = kvp.Key;
            }
        }

        return (bestCategory, bestConfidence);
    }

    private static bool MatchesAnyKeyword(string text, IReadOnlyList<string> keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static int CountMatchedKeywords(string text, IReadOnlyList<string> keywords)
    {
        return keywords.Count(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> FindMatchedKeywords(string searchText, Category category)
    {
        var matched = new List<string>();

        if (CategoryKeywords.TryGetValue(category, out IReadOnlyList<string>? keywords))
        {
            matched.AddRange(keywords.Where(keyword => searchText.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
        }

        return matched;
    }

    private static string GenerateReasoning(Category category, Priority priority, double confidence, IReadOnlyList<string> keywords)
    {
        if (category == Category.Other)
        {
            return "Classified as other with default medium priority. No confident category keywords found.";
        }

        string categoryName = category.ToString();
        string categorySnake = System.Text.RegularExpressions.Regex.Replace(categoryName, "(?<!^)(?=[A-Z])", "_").ToUpperInvariant();
        string keywordList = string.Join(", ", keywords);

        return $"Classified as {categorySnake} ({Math.Round(confidence * 100, 0):F0}% confidence) with {priority.ToString().ToUpperInvariant()} priority. Matched keywords: {keywordList}.";
    }
}
