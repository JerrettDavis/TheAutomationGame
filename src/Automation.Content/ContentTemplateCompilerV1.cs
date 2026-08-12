using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Automation.Content;

public static partial class ContentTemplateCompilerV1
{
    public const int TemplateSchemaVersion = 1;

    public static ContentTemplateV1 CompileFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Compile(File.ReadAllText(path), Path.GetFullPath(path));
    }

    public static ContentTemplateV1 Compile(string yaml, string source = "<template>")
    {
        ArgumentNullException.ThrowIfNull(yaml);
        RawTemplate raw;
        try
        {
            raw = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithDuplicateKeyChecking()
                .Build()
                .Deserialize<RawTemplate>(yaml) ?? new RawTemplate();
        }
        catch (YamlException exception)
        {
            throw new ContentCompilationException([
                new(source, "$", exception.InnerException?.Message ?? exception.Message,
                    checked((int)exception.Start.Line + 1), checked((int)exception.Start.Column + 1)),
            ]);
        }

        var diagnostics = new List<ContentDiagnostic>();
        if (raw.TemplateSchemaVersion != TemplateSchemaVersion)
            Error("template_schema_version", $"Unsupported template schema version {raw.TemplateSchemaVersion?.ToString(CultureInfo.InvariantCulture) ?? "<missing>"}; expected {TemplateSchemaVersion}.");
        if (!ContentId.IsValid(raw.TemplateId) || !raw.TemplateId!.StartsWith("template.", StringComparison.Ordinal))
            Error("template_id", $"'{raw.TemplateId ?? "<missing>"}' is not a valid 'template.' content ID.");
        if (raw.TemplateVersion is null or <= 0) Error("template_version", "Template version must be a positive integer.");
        if (string.IsNullOrWhiteSpace(raw.Content)) Error("content", "Template content is required.");

        var parameters = ImmutableSortedDictionary.CreateBuilder<string, ContentTemplateParameterDefinition>(StringComparer.Ordinal);
        foreach (var (name, kindText) in raw.Parameters)
        {
            if (!NamePattern().IsMatch(name)) { Error($"parameters.{name}", "Parameter name must use lowercase kebab-case token syntax."); continue; }
            if (!TryKind(kindText, out var kind)) { Error($"parameters.{name}", $"Unknown parameter kind '{kindText}'."); continue; }
            parameters[name] = new(name, kind);
        }

        var variants = ImmutableSortedDictionary.CreateBuilder<string, ContentTemplateVariantDefinition>(StringComparer.Ordinal);
        foreach (var (name, rawVariant) in raw.Variants)
        {
            if (!NamePattern().IsMatch(name)) { Error($"variants.{name}", "Variant name must use lowercase kebab-case token syntax."); continue; }
            if (!TryKind(rawVariant.Kind, out var kind)) { Error($"variants.{name}.kind", $"Unknown parameter kind '{rawVariant.Kind ?? "<missing>"}'."); continue; }
            if (rawVariant.Options.Count == 0) { Error($"variants.{name}.options", "Variant requires at least one option."); continue; }
            var normalized = rawVariant.Options.Select((option, index) =>
                Normalize(kind, option, source, $"variants.{name}.options[{index}]", diagnostics)).ToImmutableArray();
            if (normalized.Distinct(StringComparer.Ordinal).Count() != normalized.Length)
                Error($"variants.{name}.options", "Normalized variant options must be unique.");
            variants[name] = new(name, kind, normalized);
        }

        if (!string.IsNullOrWhiteSpace(raw.Content))
        {
            var referencedParameters = new HashSet<string>(StringComparer.Ordinal);
            var referencedVariants = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in PlaceholderPattern().Matches(raw.Content))
            {
                var category = match.Groups[1].Value;
                var name = match.Groups[2].Value;
                if (category == "parameter")
                {
                    referencedParameters.Add(name);
                    if (!parameters.ContainsKey(name)) Error("content", $"Undeclared parameter placeholder '{name}'.");
                }
                else
                {
                    referencedVariants.Add(name);
                    if (!variants.ContainsKey(name)) Error("content", $"Undeclared variant placeholder '{name}'.");
                }
            }
            foreach (var name in parameters.Keys)
                if (!referencedParameters.Contains(name)) Error($"parameters.{name}", "Declared parameter is unused by template content.");
            foreach (var name in variants.Keys)
                if (!referencedVariants.Contains(name)) Error($"variants.{name}", "Declared variant is unused by template content.");
            var recognized = PlaceholderPattern().Replace(raw.Content, "");
            if (recognized.Contains("{{", StringComparison.Ordinal) || recognized.Contains("}}", StringComparison.Ordinal))
                Error("content", "Template content contains a malformed placeholder.");
        }

        if (diagnostics.Count > 0) throw new ContentCompilationException(diagnostics);
        return new(new(raw.TemplateId!), raw.TemplateVersion!.Value, parameters.ToImmutable(), variants.ToImmutable(),
            EnsureTrailingNewline(NormalizeLineEndings(raw.Content!)), source);

        void Error(string path, string message) => diagnostics.Add(new(source, path, message));
    }

    internal static string Normalize(
        ContentTemplateParameterKind kind,
        string? value,
        string source,
        string path,
        List<ContentDiagnostic> diagnostics)
    {
        var candidate = value ?? "";
        switch (kind)
        {
            case ContentTemplateParameterKind.Token:
                if (NamePattern().IsMatch(candidate)) return candidate;
                break;
            case ContentTemplateParameterKind.ContentId:
                if (ContentId.IsValid(candidate)) return candidate;
                break;
            case ContentTemplateParameterKind.NonNegativeInteger:
                if (int.TryParse(candidate, NumberStyles.None, CultureInfo.InvariantCulture, out var nonNegative) && nonNegative >= 0)
                    return nonNegative.ToString(CultureInfo.InvariantCulture);
                break;
            case ContentTemplateParameterKind.PositiveInteger:
                if (int.TryParse(candidate, NumberStyles.None, CultureInfo.InvariantCulture, out var positive) && positive > 0)
                    return positive.ToString(CultureInfo.InvariantCulture);
                break;
            case ContentTemplateParameterKind.Boolean:
                if (bool.TryParse(candidate, out var boolean)) return boolean ? "true" : "false";
                break;
            case ContentTemplateParameterKind.Text:
                if (!string.IsNullOrWhiteSpace(candidate)) return NormalizeLineEndings(candidate);
                break;
        }
        diagnostics.Add(new(source, path, $"Value '{candidate}' is invalid for parameter kind {KindToken(kind)}."));
        return candidate;
    }

    private static string NormalizeLineEndings(string value) => value.ReplaceLineEndings("\n");
    private static string EnsureTrailingNewline(string value) => value.EndsWith('\n') ? value : value + "\n";

    private static bool TryKind(string? value, out ContentTemplateParameterKind kind)
    {
        kind = value switch
        {
            "token" => ContentTemplateParameterKind.Token,
            "content_id" => ContentTemplateParameterKind.ContentId,
            "non_negative_integer" => ContentTemplateParameterKind.NonNegativeInteger,
            "positive_integer" => ContentTemplateParameterKind.PositiveInteger,
            "boolean" => ContentTemplateParameterKind.Boolean,
            "text" => ContentTemplateParameterKind.Text,
            _ => default,
        };
        return value is "token" or "content_id" or "non_negative_integer" or "positive_integer" or "boolean" or "text";
    }

    private static string KindToken(ContentTemplateParameterKind kind) => kind switch
    {
        ContentTemplateParameterKind.Token => "token",
        ContentTemplateParameterKind.ContentId => "content_id",
        ContentTemplateParameterKind.NonNegativeInteger => "non_negative_integer",
        ContentTemplateParameterKind.PositiveInteger => "positive_integer",
        ContentTemplateParameterKind.Boolean => "boolean",
        ContentTemplateParameterKind.Text => "text",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    [GeneratedRegex("^[a-z][a-z0-9-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex NamePattern();
    [GeneratedRegex("\\{\\{(parameter|variant):([a-z][a-z0-9-]*)\\}\\}", RegexOptions.CultureInvariant)]
    internal static partial Regex PlaceholderPattern();

    private sealed class RawTemplate
    {
        public int? TemplateSchemaVersion { get; set; }
        public string? TemplateId { get; set; }
        public int? TemplateVersion { get; set; }
        public Dictionary<string, string?> Parameters { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, RawVariant> Variants { get; set; } = new(StringComparer.Ordinal);
        public string? Content { get; set; }
    }

    private sealed class RawVariant
    {
        public string? Kind { get; set; }
        public List<string?> Options { get; set; } = [];
    }
}

internal static class ContentTemplateExpanderV1
{
    public static ContentTemplateExpansionResultV1 Expand(
        ContentTemplateV1 template,
        IReadOnlyDictionary<string, string> suppliedParameters,
        string? namedSeed)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(suppliedParameters);
        var diagnostics = new List<ContentDiagnostic>();
        foreach (var name in template.Parameters.Keys)
            if (!suppliedParameters.ContainsKey(name)) diagnostics.Add(new(template.Source, $"parameters.{name}", "Required template parameter was not supplied."));
        foreach (var name in suppliedParameters.Keys)
            if (!template.Parameters.ContainsKey(name)) diagnostics.Add(new(template.Source, $"parameters.{name}", "Template parameter is not declared."));
        if (template.Variants.Count > 0 && string.IsNullOrWhiteSpace(namedSeed))
            diagnostics.Add(new(template.Source, "named_seed", "Named seed is required because the template declares variable fields."));
        if (template.Variants.Count == 0 && !string.IsNullOrWhiteSpace(namedSeed))
            diagnostics.Add(new(template.Source, "named_seed", "Named seed is only allowed when the template declares variable fields."));

        var normalizedParameters = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var (name, definition) in template.Parameters)
            if (suppliedParameters.TryGetValue(name, out var value))
                normalizedParameters[name] = ContentTemplateCompilerV1.Normalize(definition.Kind, value, template.Source,
                    $"parameters.{name}", diagnostics);
        if (diagnostics.Count > 0) throw new ContentCompilationException(diagnostics);

        var selections = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var (name, definition) in template.Variants)
        {
            var input = Encoding.UTF8.GetBytes($"template-v1\n{template.Id}\n{template.Version}\n{namedSeed}\n{name}");
            var digest = SHA256.HashData(input);
            var index = BinaryPrimitives.ReadUInt64BigEndian(digest) % (ulong)definition.Options.Length;
            selections[name] = definition.Options[(int)index];
        }

        var expanded = template.Content;
        foreach (var (name, value) in normalizedParameters)
            expanded = expanded.Replace($"{{{{parameter:{name}}}}}", value, StringComparison.Ordinal);
        foreach (var (name, value) in selections)
            expanded = expanded.Replace($"{{{{variant:{name}}}}}", value, StringComparison.Ordinal);
        if (ContentTemplateCompilerV1.PlaceholderPattern().IsMatch(expanded) || expanded.Contains("{{", StringComparison.Ordinal))
            throw new ContentCompilationException([new(template.Source, "content", "Expansion left an unresolved template placeholder.")]);

        var catalog = ContentCompilerV1.Compile(expanded, $"{template.Source}#expanded");
        var provenance = new ContentTemplateProvenanceV1(template.Source, template.Id, template.Version,
            string.IsNullOrWhiteSpace(namedSeed) ? null : namedSeed, normalizedParameters.ToImmutable(), selections.ToImmutable());
        var canonical = new StringBuilder()
            .AppendLine("expansion|1")
            .AppendLine($"template|{template.Id}|{template.Version}")
            .AppendLine($"seed|{Convert.ToBase64String(Encoding.UTF8.GetBytes(namedSeed ?? ""))}");
        foreach (var (name, value) in normalizedParameters)
            canonical.AppendLine($"parameter|{name}|{Convert.ToBase64String(Encoding.UTF8.GetBytes(value))}");
        foreach (var (name, value) in selections)
            canonical.AppendLine($"variant|{name}|{Convert.ToBase64String(Encoding.UTF8.GetBytes(value))}");
        canonical.AppendLine($"content|{catalog.Manifest.Sha256}");
        var expansionHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
        return new(expanded, catalog, provenance, expansionHash);
    }
}
