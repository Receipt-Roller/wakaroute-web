namespace wakaroute_web.Services.Manabu2;

public sealed class Manabu2Options
{
    public const string SectionName = "Manabu2";

    public string BaseUrl { get; set; } = "https://api.manabu2.com";

    public string OrganizationId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public int CacheMinutes { get; set; } = 10;

    public bool IsConfigured =>
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out _) &&
        !string.IsNullOrWhiteSpace(OrganizationId) &&
        !string.IsNullOrWhiteSpace(ApiKey);
}
