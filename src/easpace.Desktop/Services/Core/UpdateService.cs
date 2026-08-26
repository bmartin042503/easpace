// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace easpace.Desktop.Services.Core;

internal record UpdateCheckResult(bool IsUpdateAvailable, Version? LatestVersion, string? ReleaseUrl);

internal class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;

    public UpdateService()
    {
        _httpClient = new HttpClient();
        
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("easpace", App.Version.ToString()));
        
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        const string owner = "bmartin042503";
        const string repo = "easpace";
        
        const string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            var response = await _httpClient.GetAsync(url, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(false, null, null);
            }

            var jsonString = await response.Content.ReadAsStringAsync(cts.Token);
            var release = JsonSerializer.Deserialize<GitHubRelease>(jsonString);

            if (release != null && !string.IsNullOrEmpty(release.TagName))
            {
                var cleanVersion = release.TagName.TrimStart('v', 'V');

                if (Version.TryParse(cleanVersion, out var latestVersion))
                {
                    var isUpdateAvailable = latestVersion > App.Version;
                    
                    return new UpdateCheckResult(isUpdateAvailable, latestVersion, release.HtmlUrl);
                }
            }
        }
        catch (HttpRequestException)
        {
            // no internet or GitHub isn't available - ignored
        }
        catch (TaskCanceledException)
        {
            // timeout - ignored
        }
        catch (Exception)
        {
            // ignored
        }
        
        return new UpdateCheckResult(false, null, null);
    }
    
    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
    }
}