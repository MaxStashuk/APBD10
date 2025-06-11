using System.Text.Json.Serialization;

namespace API.Models;

public class ValidationRule
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("preRequestName")]
    public string PreRequestName { get; set; } = string.Empty;

    [JsonPropertyName("preRequestValue")]
    public string PreRequestValue { get; set; } = string.Empty;

    [JsonPropertyName("rules")]
    public List<PropertyRule> Rules { get; set; } = new();
}

public class PropertyRule
{
    [JsonPropertyName("paramName")]
    public string ParamName { get; set; } = string.Empty;

    [JsonPropertyName("regex")]
    public object Regex { get; set; } = string.Empty;
} 