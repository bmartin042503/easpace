// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace easpace.Desktop.Services.Core;

internal record UpdateCheckResult(
    bool IsUpdateAvailable,
    Version? LatestVersion,
    string? ReleaseUrl,
    string? ReleaseTitle,
    string? ReleaseDescription);

internal class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;

    public UpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("easpace", App.Version.ToString()));
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        const string owner = "bmartin042503";
        const string repo = "easpace";

        const string url =
            $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var response = await _httpClient.GetAsync(url, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return NoUpdate;
            }

            var jsonString =
                await response.Content.ReadAsStringAsync(cts.Token);

            var release =
                JsonSerializer.Deserialize<GitHubRelease>(jsonString);

            if (release is null ||
                string.IsNullOrWhiteSpace(release.TagName))
            {
                return NoUpdate;
            }

            var cleanVersion =
                release.TagName.TrimStart('v', 'V');

            if (!Version.TryParse(cleanVersion, out var latestVersion))
            {
                return NoUpdate;
            }

            var isUpdateAvailable =
                latestVersion > App.Version;

            return new UpdateCheckResult(
                IsUpdateAvailable: isUpdateAvailable,
                LatestVersion: latestVersion,
                ReleaseUrl: release.HtmlUrl,
                ReleaseTitle: release.Name,
                ReleaseDescription: release.Body);
        }
        catch (HttpRequestException)
        {
            // No internet or GitHub isn't available - ignored
        }
        catch (TaskCanceledException)
        {
            // Timeout - ignored
        }
        catch (Exception)
        {
            // Ignored
        }

        return NoUpdate;
    }
    
    private static UpdateCheckResult NoUpdate =>
        new(
            IsUpdateAvailable: false,
            LatestVersion: null,
            ReleaseUrl: null,
            ReleaseTitle: null,
            ReleaseDescription: null);


    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")] public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("name")] public string? Name { get; init; }

        [JsonPropertyName("body")] public string? Body { get; init; }
    }
}