namespace Homework2.Bll.Domain;

/// <summary>Result of ticket classification.</summary>
/// <param name="Category">Classified category.</param>
/// <param name="Priority">Classified priority.</param>
/// <param name="Confidence">Confidence score (0.0 to 1.0).</param>
/// <param name="Reasoning">Human-readable reasoning for the classification.</param>
/// <param name="KeywordsFound">List of keywords found in the ticket that influenced classification.</param>
public record ClassificationResult(
    Category Category,
    Priority Priority,
    double Confidence,
    string Reasoning,
    IReadOnlyList<string> KeywordsFound
);
