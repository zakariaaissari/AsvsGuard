using System.Text.Json.Serialization;

namespace OWASPAsvs.Core.Entities;

public record SemgrepOutput(
    [property: JsonPropertyName("results")] List<SemgrepResult> Results
);

public record SemgrepResult(
    [property: JsonPropertyName("check_id")] string CheckId,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("start")] SemgrepLocation Start,
    [property: JsonPropertyName("extra")] SemgrepExtra Extra
);

public record SemgrepLocation(
    [property: JsonPropertyName("line")] int Line
);

public record SemgrepExtra(
    [property: JsonPropertyName("message")] string Message
);
