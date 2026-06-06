namespace RentAll.Domain.Models.ESignature;

public class DocuSignSigner
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RoutingOrder { get; set; } = 1;
}
