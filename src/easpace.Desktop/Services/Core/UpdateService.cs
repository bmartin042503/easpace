// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
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
            $"https://api.github.com/repos/{owner}/{repo}/releases?per_page=100";

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var response = await _httpClient.GetAsync(url, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return NoUpdate;
            }

            var jsonString = await response.Content.ReadAsStringAsync(cts.Token);

            var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(jsonString);

            if (releases is null || releases.Count == 0)
            {
                return NoUpdate;
            }

            GitHubRelease? latestRelease = null;
            Version? latestVersion = null;

            foreach (var release in releases)
            {
                if (release.Draft || string.IsNullOrWhiteSpace(release.TagName)) continue;

                var cleanVersion = release.TagName.Trim().TrimStart('v', 'V');

                if (!Version.TryParse(cleanVersion, out var version)) continue;

                if (latestVersion is null || version > latestVersion)
                {
                    latestVersion = version;
                    latestRelease = release;
                }
            }

            if (latestRelease is null || latestVersion is null) return NoUpdate;

            var isUpdateAvailable = latestVersion > App.Version;

            return new UpdateCheckResult(
                IsUpdateAvailable: isUpdateAvailable,
                LatestVersion: latestVersion,
                ReleaseUrl: latestRelease.HtmlUrl,
                ReleaseTitle: latestRelease.Name,
                ReleaseDescription: latestRelease.Body);
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
        new(IsUpdateAvailable: false,
            LatestVersion: null,
            ReleaseUrl: null,
            ReleaseTitle: null,
            ReleaseDescription: null);


    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string TagName { get; init; } = string.Empty;
        [JsonPropertyName("html_url")] public string HtmlUrl { get; init; } = string.Empty;
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("body")] public string? Body { get; init; }
        [JsonPropertyName("draft")] public bool Draft { get; init; }
        [JsonPropertyName("prerelease")] public bool Prerelease { get; init; }
    }
}