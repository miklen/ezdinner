namespace EzDinner.Core.DomainServices.DishRecommendations;

public enum IngredientEvidenceKind { Fact, Inference }
public enum IngredientRole { Primary, Incidental }

public sealed record DishIngredientMatchValueObject
{
    public string Key { get; }
    public double Importance { get; }
    public double Confidence { get; }
    public IngredientEvidenceKind EvidenceKind { get; }
    public IReadOnlyList<string> SourceReferences { get; }
    public string? SupportingExcerpt { get; }
    public IngredientRole Role { get; }

    public DishIngredientMatchValueObject(string key, double importance, double confidence,
        IngredientEvidenceKind evidenceKind, IReadOnlyList<string> sourceReferences,
        string? supportingExcerpt = null, IngredientRole role = IngredientRole.Primary)
    {
        if (!IngredientVocabularyFactory.IsKnown(key)) throw new ArgumentException("UNKNOWN_INGREDIENT");
        if (!double.IsFinite(importance) || importance is < 0 or > 1 ||
            !double.IsFinite(confidence) || confidence is < 0 or > 1) throw new ArgumentException("INVALID_INGREDIENT_WEIGHT");
        if (!Enum.IsDefined(evidenceKind) || !Enum.IsDefined(role)) throw new ArgumentException("INVALID_INGREDIENT_EVIDENCE");
        if (sourceReferences.Any(string.IsNullOrWhiteSpace) ||
            (evidenceKind == IngredientEvidenceKind.Fact && (sourceReferences.Count == 0 || string.IsNullOrWhiteSpace(supportingExcerpt))))
            throw new ArgumentException("INGREDIENT_FACT_REQUIRES_EVIDENCE");
        Key = key;
        Role = IngredientVocabularyFactory.IsIncidental(key) ? IngredientRole.Incidental : role;
        Importance = Role == IngredientRole.Incidental ? 0 : importance;
        Confidence = confidence;
        EvidenceKind = evidenceKind;
        SourceReferences = Array.AsReadOnly(sourceReferences.Distinct(StringComparer.Ordinal).ToArray());
        SupportingExcerpt = supportingExcerpt;
    }
}
