using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;
using RentAll.Infrastructure.Entities;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Emails
{
	public partial class EmailRepository : IEmailRepository
	{
		private readonly string _dbConnectionString;

		public EmailRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections
				.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!
				.ConnectionString;
		}

		private Email ConvertEntityToModel(EmailEntity e)
		{
			return new Email
			{
				EmailId = e.EmailId,
				OrganizationId = e.OrganizationId,
				OfficeId = e.OfficeId,
				PropertyId = e.PropertyId,
				ReservationId = e.ReservationId,
				ToRecipients = DeserializeRecipients(e.ToRecipients),
				CcRecipients = DeserializeRecipients(e.CcRecipients),
				BccRecipients = DeserializeRecipients(e.BccRecipients),
				FromRecipient = DeserializeRecipient(e.FromRecipient),
				Subject = e.Subject,
				PlainTextContent = e.PlainTextContent,
				HtmlContent = e.HtmlContent,
				DocumentId = e.DocumentId,
				AttachmentName = e.AttachmentName,
				AttachmentPath = e.AttachmentPath,
				EmailType = (EmailType)e.EmailTypeId,
				EmailStatus = (EmailStatus)e.EmailStatusId,
				AttemptCount = e.AttemptCount,
				LastError = e.LastError,
				LastAttemptedOn = e.LastAttemptedOn,
				SentOn = e.SentOn,
				CreatedOn = e.CreatedOn,
				CreatedBy = e.CreatedBy,
				ModifiedOn = e.ModifiedOn,
				ModifiedBy = e.ModifiedBy
			};
		}

		private static readonly JsonSerializerOptions SerializerOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		private static List<EmailAddress> DeserializeRecipients(string recipientsJson)
		{
			if (string.IsNullOrWhiteSpace(recipientsJson))
				return [];

			try
			{
				var values = JsonSerializer.Deserialize<List<EmailAddressEntity>>(recipientsJson, SerializerOptions) ?? [];
				return values
					.Select(recipient => new EmailAddress
					{
						Email = recipient.Email,
						Name = recipient.Name
					})
					.ToList();
			}
			catch
			{
				return [];
			}
		}

		private static EmailAddress DeserializeRecipient(string recipientJson)
		{
			if (string.IsNullOrWhiteSpace(recipientJson))
				return new EmailAddress();

			try
			{
				var value = JsonSerializer.Deserialize<EmailAddressEntity>(recipientJson, SerializerOptions);
				return value == null
					? new EmailAddress()
					: new EmailAddress
					{
						Email = value.Email,
						Name = value.Name
					};
			}
			catch
			{
				return new EmailAddress();
			}
		}

		private static string SerializeRecipients(IEnumerable<EmailAddress>? recipients)
		{
			var entityValues = (recipients ?? Enumerable.Empty<EmailAddress>()).Select(recipient => new EmailAddressEntity
			{
				Email = recipient.Email,
				Name = recipient.Name
			});
			return JsonSerializer.Serialize(entityValues, SerializerOptions);
		}

		private static string SerializeRecipient(EmailAddress? recipient)
		{
			var entityValue = new EmailAddressEntity
			{
				Email = recipient?.Email ?? string.Empty,
				Name = recipient?.Name ?? string.Empty
			};
			return JsonSerializer.Serialize(entityValue, SerializerOptions);
		}
	}
}
