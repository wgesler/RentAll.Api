namespace RentAll.Domain.Configuration
{
	public class AppSettings
	{
		public string[] AllowedHostNames { get; set; } = Array.Empty<string>();
		public string Environment { get; set; } = string.Empty;
		public List<DbConnection> DbConnections { get; set; } = new List<DbConnection>();
		public List<EmailRecipient> EmailRecipients { get; set; } = new List<EmailRecipient>();
		public List<ServiceConnection> ServiceConnections { get; set; } = new List<ServiceConnection>();
	}


	public class DbConnection
	{
		public string DbName { get; set; } = string.Empty;
		public string ConnectionString { get; set; } = string.Empty;
	}

	public class EmailRecipient
	{
		public string Name { get; set; } = string.Empty;
		public string EmailAddress { get; set; } = string.Empty;
		public string DisplayName { get; set; } = string.Empty;
		public string ReplyTo { get; set; } = string.Empty;
	}

	public class ServiceConnection
	{
		public string ServiceName { get; set; } = string.Empty;
		public string ServiceURL { get; set; } = string.Empty;
	}
}

