using System.Text.Json.Serialization;

namespace Bellwood.AdminPortal.Models;

// ?????????????????????????????????????????????????????????????????????????????
// Booker profile  (GET /profile  /  PUT /profile)
// ?????????????????????????????????????????????????????????????????????????????

/// <summary>
/// Response from GET /profile — the caller's (or a booker's) profile record.
/// </summary>
public class BookerProfileDto
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("createdUtc")]
    public DateTime? CreatedUtc { get; set; }

    [JsonPropertyName("modifiedUtc")]
    public DateTime? ModifiedUtc { get; set; }
}

// ?????????????????????????????????????????????????????????????????????????????
// Saved passengers  (GET|POST /profile/passengers,  PUT|DELETE /profile/passengers/{id})
// ?????????????????????????????????????????????????????????????????????????????

/// <summary>
/// A saved passenger record returned by the API.
/// </summary>
public class SavedPassengerDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("modifiedUtc")]
    public DateTime? ModifiedUtc { get; set; }

    /// <summary>Convenience: "FirstName LastName".</summary>
    [JsonIgnore]
    public string DisplayName => $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// Body for POST /profile/passengers and PUT /profile/passengers/{id}.
/// firstName and lastName are required; blank strings are trimmed to null before sending.
/// </summary>
public class SavedPassengerRequest
{
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Null / empty is allowed; blank strings are trimmed to null by the service.</summary>
    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    /// <summary>Null / empty is allowed; blank strings are trimmed to null by the service.</summary>
    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }
}

// ?????????????????????????????????????????????????????????????????????????????
// Saved locations  (GET|POST /profile/locations,  PUT|DELETE /profile/locations/{id})
// ?????????????????????????????????????????????????????????????????????????????

/// <summary>
/// A saved location record returned by the API.
/// </summary>
public class SavedLocationDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("isFavorite")]
    public bool IsFavorite { get; set; }

    /// <summary>Server-managed; cannot be overridden via PUT.</summary>
    [JsonPropertyName("useCount")]
    public int UseCount { get; set; }

    [JsonPropertyName("modifiedUtc")]
    public DateTime? ModifiedUtc { get; set; }
}

/// <summary>
/// Body for POST /profile/locations.
/// All five fields are required by the API.
/// For PUT, the service fetches the current record first so useCount is preserved server-side.
/// </summary>
public class SavedLocationRequest
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("isFavorite")]
    public bool IsFavorite { get; set; }
}
