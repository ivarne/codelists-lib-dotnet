﻿using Altinn.Codelists.Extensions;
using Microsoft.Extensions.Options;

namespace Altinn.Codelists.SSB.Clients;

/// <summary>
/// Http client to get classification codes from SSB.
/// </summary>
public class ClassificationsHttpClient : IClassificationsClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassificationsHttpClient"/> class.
    /// </summary>
    public ClassificationsHttpClient(IOptions<ClassificationSettings> settings, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(settings.Value.BaseApiUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json;charset=utf-8");
    }

    /// <summary>
    /// Gets the codes for the specified classification.
    /// </summary>
    /// <param name="classificationId">The id of the classification to get</param>
    /// <param name="language">The language code used for the labels. Valid options are nb (norsk bokmål), nn (nynorsk) and en (english)
    /// Default if nothing is specified is nb (norsk bokmål).
    /// </param>
    /// <param name="atDate">The date the classification should be valid</param>
    /// <param name="level">The hierarchy level for classifications with multiple levels. Defaults to empty string, ie. all levels.</param>
    /// <returns></returns>
    public async Task<ClassificationCodes> GetClassificationCodes(int classificationId, string language="nb", DateOnly? atDate = null, string level = "", string variant = "")
    {
        string selectLanguage = $"language={language}";

        // If no date is specified we use todays date to get the latest classification codes.
        DateOnly date = atDate ?? DateOnly.FromDateTime(DateTime.Today);
        string selectDate = $"&date={date.ToString("yyyy-MM-dd")}";

        // No level specified means all levels will be returned
        string selectLevel = level == string.Empty ? string.Empty : $"&selectLevel={level}";

        // Variants are referenced by name
        string selectVariant = variant.IsNullOrEmpty() ? string.Empty : $"&variantName={variant}";

        // Start of url differs depending on if we are getting codes or variants
        string url = $"{classificationId}/codesAt";
        if (!variant.IsNullOrEmpty())
        {
            url = $"{classificationId}/variantAt";
        }

        string query = $"?{selectLanguage}{selectDate}{selectLevel}{selectVariant}";

        var response = await _httpClient.GetAsync($"{url}{query}");
        var responseJson = await response.Content.ReadAsStringAsync();

        var classificationCodes = JsonSerializer.Deserialize<ClassificationCodes>(responseJson);

        return classificationCodes ?? new ClassificationCodes();
    }
}
