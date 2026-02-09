namespace RentAll.Domain.Configuration;

public class StorageSettings
{
	/// <summary>
	/// Storage provider type: "FileSystem" or "AzureBlob"
	/// </summary>
	public string Provider { get; set; } = "FileSystem";

	/// <summary>
	/// Azure Blob Storage connection string (optional, used if provided)
	/// If not provided, will use managed identity authentication
	/// </summary>
	public string? AzureBlobConnectionString { get; set; }

	/// <summary>
	/// Base URL for Azure Blob Storage (required when using managed identity, optional when using connection string)
	/// Used for authentication endpoint when using managed identity, and for generating public URLs
	/// Example: https://rentallstorage.blob.core.windows.net
	/// </summary>
	public string? AzureBlobBaseUrl { get; set; }
}
