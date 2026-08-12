using System.Collections.Immutable;

namespace Automation.Content;

public enum ContentTemplateParameterKind
{
    Token,
    ContentId,
    NonNegativeInteger,
    PositiveInteger,
    Boolean,
    Text,
}

public sealed record ContentTemplateParameterDefinition(
    string Name,
    ContentTemplateParameterKind Kind);

public sealed record ContentTemplateVariantDefinition(
    string Name,
    ContentTemplateParameterKind Kind,
    ImmutableArray<string> Options);

public sealed record ContentTemplateProvenanceV1(
    string Source,
    ContentId TemplateId,
    int TemplateVersion,
    string? NamedSeed,
    ImmutableSortedDictionary<string, string> Parameters,
    ImmutableSortedDictionary<string, string> VariantSelections);

public sealed record ContentTemplateExpansionResultV1(
    string ExpandedYaml,
    CompiledContentCatalogV1 Catalog,
    ContentTemplateProvenanceV1 Provenance,
    string ExpansionSha256);

public interface IContentTemplateV1
{
    ContentId Id { get; }
    int Version { get; }
    ContentTemplateExpansionResultV1 Expand(IReadOnlyDictionary<string, string> parameters, string? namedSeed = null);
}

public sealed record ContentTemplateV1(
    ContentId Id,
    int Version,
    ImmutableSortedDictionary<string, ContentTemplateParameterDefinition> Parameters,
    ImmutableSortedDictionary<string, ContentTemplateVariantDefinition> Variants,
    string Content,
    string Source) : IContentTemplateV1
{
    public ContentTemplateExpansionResultV1 Expand(IReadOnlyDictionary<string, string> parameters, string? namedSeed = null) =>
        ContentTemplateExpanderV1.Expand(this, parameters, namedSeed);
}
