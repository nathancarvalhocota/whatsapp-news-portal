using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Sources.Application;

public class SourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public static SourceDto FromSource(Source s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Type = s.Type.ToString(),
        BaseUrl = s.BaseUrl,
        IsActive = s.IsActive
    };
}
