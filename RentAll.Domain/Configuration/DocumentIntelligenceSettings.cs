namespace RentAll.Domain.Configuration;

public class DocumentIntelligenceSettings
{
    public bool Enabled { get; set; }

    /// <summary>
    /// Azure Document Intelligence endpoint,
    /// e.g. https://rentall-document-intelligence.cognitiveservices.azure.com/.
    /// </summary>
    public string? Endpoint { get; set; }
}
