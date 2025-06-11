using System.Text.Json;
using API.Models;

namespace API.Services;

public interface IValidationService
{
    Task<List<ValidationRule>> LoadValidationRulesAsync();
    bool ValidateObject(object obj, string type);
}

public class ValidationService : IValidationService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ValidationService> _logger;
    private List<ValidationRule>? _cachedRules;

    public ValidationService(IWebHostEnvironment environment, ILogger<ValidationService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<List<ValidationRule>> LoadValidationRulesAsync()
    {
        if (_cachedRules != null)
        {
            _logger.LogDebug("Using cached validation rules");
            return _cachedRules;
        }

        var rulesPath = Path.Combine(_environment.ContentRootPath, "..", "validation_rules", "example_validation_rules.json");
        _logger.LogInformation("Loading validation rules from {Path}", rulesPath);

        try
        {
            var jsonContent = await File.ReadAllTextAsync(rulesPath);
            var rules = JsonSerializer.Deserialize<ValidationRulesWrapper>(jsonContent);
            _cachedRules = rules?.Validations ?? new List<ValidationRule>();
            _logger.LogInformation("Successfully loaded {Count} validation rules", _cachedRules.Count);
            return _cachedRules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load validation rules from {Path}", rulesPath);
            throw;
        }
    }

    public bool ValidateObject(object obj, string type)
    {
        _logger.LogDebug("Starting validation for type {Type}", type);
        var rules = LoadValidationRulesAsync().GetAwaiter().GetResult();
        var typeRules = rules.FirstOrDefault(r => r.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

        if (typeRules == null)
        {
            _logger.LogDebug("No validation rules found for type {Type}", type);
            return true;
        }

        // Check pre-request condition
        var preRequestProperty = obj.GetType().GetProperty(typeRules.PreRequestName);
        if (preRequestProperty == null)
        {
            _logger.LogWarning("Pre-request property {Property} not found for type {Type}", 
                typeRules.PreRequestName, type);
            return false;
        }

        var preRequestValue = preRequestProperty.GetValue(obj)?.ToString();
        if (preRequestValue != typeRules.PreRequestValue)
        {
            _logger.LogWarning("Pre-request condition not met for type {Type}. Expected: {Expected}, Got: {Actual}", 
                type, typeRules.PreRequestValue, preRequestValue);
            return false;
        }

        // Validate each rule
        foreach (var rule in typeRules.Rules)
        {
            var property = obj.GetType().GetProperty(rule.ParamName);
            if (property == null)
            {
                _logger.LogDebug("Property {Property} not found in object, skipping validation", rule.ParamName);
                continue;
            }

            var value = property.GetValue(obj)?.ToString();
            if (value == null)
            {
                _logger.LogDebug("Property {Property} has null value, skipping validation", rule.ParamName);
                continue;
            }

            if (rule.Regex is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    var allowedValues = jsonElement.EnumerateArray()
                        .Select(e => e.GetString())
                        .Where(s => s != null)
                        .ToList();

                    if (!allowedValues.Contains(value))
                    {
                        _logger.LogWarning("Validation failed for property {Property}. Value {Value} not in allowed values: {AllowedValues}", 
                            rule.ParamName, value, allowedValues);
                        return false;
                    }
                }
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    var pattern = jsonElement.GetString();
                    if (pattern != null && !System.Text.RegularExpressions.Regex.IsMatch(value, pattern))
                    {
                        _logger.LogWarning("Validation failed for property {Property}. Value {Value} does not match pattern {Pattern}", 
                            rule.ParamName, value, pattern);
                        return false;
                    }
                }
            }
        }

        _logger.LogInformation("Validation successful for type {Type}", type);
        return true;
    }
}

public class ValidationRulesWrapper
{
    public List<ValidationRule> Validations { get; set; } = new();
} 