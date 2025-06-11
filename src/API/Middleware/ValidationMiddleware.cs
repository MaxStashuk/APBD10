using System.Text.Json;
using API.Services;
using Microsoft.Extensions.Logging;

namespace API.Middleware;

public class ValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IValidationService _validationService;
    private readonly ILogger<ValidationMiddleware> _logger;

    public ValidationMiddleware(RequestDelegate next, IValidationService validationService, ILogger<ValidationMiddleware> logger)
    {
        _next = next;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("Starting validation middleware for {Method} request to {Path}", 
            context.Request.Method, context.Request.Path);

        // Only validate POST and PUT requests
        if (context.Request.Method != "POST" && context.Request.Method != "PUT")
        {
            _logger.LogDebug("Skipping validation for non-POST/PUT request");
            await _next(context);
            return;
        }

        // Get the type from the route
        var type = context.Request.RouteValues["type"]?.ToString();
        if (string.IsNullOrEmpty(type))
        {
            _logger.LogDebug("No type found in route values, skipping validation");
            await _next(context);
            return;
        }

        _logger.LogInformation("Validating request for type: {Type}", type);

        // Read the request body
        context.Request.EnableBuffering();
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (string.IsNullOrEmpty(requestBody))
        {
            _logger.LogDebug("Empty request body, skipping validation");
            await _next(context);
            return;
        }

        try
        {
            // Deserialize the request body to a dynamic object
            var jsonDocument = JsonDocument.Parse(requestBody);
            var rootElement = jsonDocument.RootElement;

            // Check if additionalProperties exists
            if (!rootElement.TryGetProperty("additionalProperties", out var additionalPropertiesElement))
            {
                _logger.LogDebug("No additionalProperties found in request body");
                await _next(context);
                return;
            }

            if (additionalPropertiesElement.ValueKind != JsonValueKind.Object)
            {
                _logger.LogWarning("additionalProperties is not an object");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "additionalProperties must be an object" });
                return;
            }

            var additionalProperties = new Dictionary<string, object>();
            foreach (var property in additionalPropertiesElement.EnumerateObject())
            {
                additionalProperties[property.Name] = property.Value.GetString() ?? string.Empty;
            }

            _logger.LogDebug("Found {Count} additional properties to validate", additionalProperties.Count);

            // Create a dynamic object with the additional properties
            var dynamicObject = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
            foreach (var prop in additionalProperties)
            {
                dynamicObject[prop.Key] = prop.Value;
            }

            // Validate the object
            if (!_validationService.ValidateObject(dynamicObject, type))
            {
                _logger.LogWarning("Validation failed for type {Type}. Invalid additional properties: {@Properties}", 
                    type, additionalProperties);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Validation failed for additional properties" });
                return;
            }

            _logger.LogInformation("Validation successful for type {Type}", type);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format in request body");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid JSON format" });
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation for type {Type}", type);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "Internal server error during validation" });
            return;
        }

        await _next(context);
        _logger.LogInformation("Validation middleware completed for {Method} request to {Path}", 
            context.Request.Method, context.Request.Path);
    }
} 