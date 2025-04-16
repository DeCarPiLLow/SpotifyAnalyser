// SpotifyAuth.cs - Controller

using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class SpotifyAuth : ControllerBase
{
    private readonly string clientId = "9f4143e8a29840ba850ea52718bcd521";
    private readonly string clientSecret = "295a041bb372449a8145ee22ef75abec";
    private readonly string redirectUri = "http://127.0.0.1:5000/api/SpotifyAuth/callback"; // your redirect URI

    [HttpGet("login")]
    public IActionResult Login()
    {
        var scope = "user-read-private user-read-email user-top-read";
        var authUrl = $"https://accounts.spotify.com/authorize?" +
                      $"response_type=code&client_id={clientId}&scope={scope}&redirect_uri={Uri.EscapeDataString(redirectUri)}";

        return Redirect(authUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? code = null, [FromQuery] string? error = null)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return BadRequest($"Error during Spotify authentication: {error}");
        }

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest("No code received from Spotify.");
        }

        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var requestBody = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", redirectUri }
        };

        var requestContent = new FormUrlEncodedContent(requestBody);
        var response = await httpClient.PostAsync("https://accounts.spotify.com/api/token", requestContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return BadRequest($"Failed to exchange code: {responseContent}");
        }

        var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
        var accessToken = tokenData["access_token"].ToString();

        // Fetch profile now using the access token
        using var profileClient = new HttpClient();
        profileClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var profileResponse = await profileClient.GetAsync("https://api.spotify.com/v1/me");

        var profileContent = await profileResponse.Content.ReadAsStringAsync();

        if (!profileResponse.IsSuccessStatusCode)
        {
            return BadRequest($"Failed to fetch profile: {profileContent}");
        }

        var userProfile = JsonSerializer.Deserialize<Dictionary<string, object>>(profileContent);

        // Return both access token and user profile
        return Ok(new { access_token = accessToken, profile = userProfile });
    }
}
