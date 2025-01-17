﻿using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using Altinn.Codelists.SSB.Models;

namespace Altinn.Codelists.SSB;

/// <summary>
/// Base class providing functions for getting codelist.
/// </summary>
public class ClassificationCodelistProvider : IAppOptionsProvider
{
    private readonly IClassificationsClient _classificationsClient;
    private readonly int _classificationId;
    private readonly Dictionary<string, string> _defaultKeyValuePairs;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassificationCodelistProvider"/> class.
    /// </summary>
    public ClassificationCodelistProvider(string id, Classification classification, IClassificationsClient classificationsClient, Dictionary<string, string>? defaultKeyValuePairs = null) : 
        this(id, (int) classification, classificationsClient, defaultKeyValuePairs)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassificationCodelistProvider"/> class.
    /// For valid id's please consult the SSB classificaionts api by calling
    /// <see href="http://data.ssb.no/api/klass/v1/classifications?size=150&language=en">the classifications index.</see>
    /// </summary>
    public ClassificationCodelistProvider(string id, int classificationId, IClassificationsClient classificationsClient, Dictionary<string, string>? defaultKeyValuePairs = null)
    {
        Id = id;
        _classificationId = classificationId;
        _classificationsClient = classificationsClient;
        _defaultKeyValuePairs = defaultKeyValuePairs == null ? new Dictionary<string, string>() : defaultKeyValuePairs;
    }

    /// <inheritdoc/>
    public string Id { get; private set; }

    /// Gets the <see cref="AppOptions"/> based on the provided classification, options id and key value pairs.
    public async Task<AppOptions> GetAppOptionsAsync(string language, Dictionary<string, string> keyValuePairs)
    {
        Dictionary<string, string> mergedKeyValuePairs = MergeDictionaries(_defaultKeyValuePairs, keyValuePairs);

        string? date = mergedKeyValuePairs.GetValueOrDefault("date");
        DateOnly dateOnly = date == null ? DateOnly.FromDateTime(DateTime.Today) : DateOnly.Parse(date);
        string level = mergedKeyValuePairs.GetValueOrDefault("level") ?? string.Empty;
        string variant = mergedKeyValuePairs.GetValueOrDefault("variant") ?? string.Empty;

        var classificationCode = await _classificationsClient.GetClassificationCodes(_classificationId, language, dateOnly, level, variant);

        string parentCode = mergedKeyValuePairs.GetValueOrDefault("parentCode") ?? string.Empty;

        AppOptions appOptions = GetAppOptions(classificationCode, parentCode);

        return appOptions;
    }

    private static AppOptions GetAppOptions(Clients.ClassificationCodes classificationCode, string parentCode)
    {
        AppOptions appOptions;
        // From Altinn.App.Core version 7.8.0 we can add description to AppOptions.
        // This is not supported in older versions, and we need to check if the property exists.
        if (AppOptionsSupportsDescription())
        {
            appOptions = new AppOptions
            {
                // The api we use doesn't support filtering on partentCode,
                // hence we need to filter afterwards.
                Options = string.IsNullOrEmpty(parentCode)
            ? classificationCode.Codes.Select(x => new AppOption() { Value = x.Code, Label = x.Name, Description = x.Notes }).ToList()
            : classificationCode.Codes.Where(c => c.ParentCode == parentCode).Select(x => new AppOption() { Value = x.Code, Label = x.Name, Description = x.Notes }).ToList()
            };
        }
        else
        {
            appOptions = new AppOptions
            {
                // The api we use doesn't support filtering on partentCode,
                // hence we need to filter afterwards.
                Options = string.IsNullOrEmpty(parentCode)
            ? classificationCode.Codes.Select(x => new AppOption() { Value = x.Code, Label = x.Name }).ToList()
            : classificationCode.Codes.Where(c => c.ParentCode == parentCode).Select(x => new AppOption() { Value = x.Code, Label = x.Name }).ToList()
            };
        }

        return appOptions;
    }

    private static bool AppOptionsSupportsDescription()
    {
        return typeof(AppOption).GetProperties().Any(x => x.Name == "Description");
    }

    private static Dictionary<string, string> MergeDictionaries(Dictionary<string, string> defaultValues, Dictionary<string, string> overridingValues)
    {
        var mergedDictionary = new Dictionary<string, string>(defaultValues); 

        foreach (var keyValuePair in overridingValues)
        {
            if (mergedDictionary.ContainsKey(keyValuePair.Key))
            {
                mergedDictionary[keyValuePair.Key] = keyValuePair.Value; 
            }
            else
            {
                mergedDictionary.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        return mergedDictionary;
    }
}