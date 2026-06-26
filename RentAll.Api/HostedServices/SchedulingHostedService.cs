using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Options;
using RentAll.Api.Logging;
using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;
using RentAll.Domain.Scheduling;

namespace RentAll.Api.HostedServices;

public class SchedulingHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SchedulingHostedService> _logger;
    private readonly ApplicationLoggingSettings _applicationLoggingSettings;
    private DateOnly? _lastRetentionRunDateUtc;

    public SchedulingHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SchedulingHostedService> logger,
        IOptions<ApplicationLoggingSettings> applicationLoggingSettings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _applicationLoggingSettings = applicationLoggingSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduling hosted service cycle failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var propertyRepository = scope.ServiceProvider.GetRequiredService<IPropertyRepository>();
        var leadRepository = scope.ServiceProvider.GetRequiredService<ILeadRepository>();
        var organizationRepository = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var emailManager = scope.ServiceProvider.GetRequiredService<IEmailManager>();
        var loggingRepository = scope.ServiceProvider.GetRequiredService<ILoggingRepository>();

        await ProcessRetireExpiredListingLinksAsync(propertyRepository, cancellationToken);
        await ProcessRetireExpiredOwnerFormLinksAsync(leadRepository, cancellationToken);
        await ProcessScheduledAlertsAsync(organizationRepository, emailRepository, reservationRepository, emailManager, cancellationToken);
        await ProcessLogRetentionAsync(loggingRepository, cancellationToken);
    }

    #region Alerts
    private async Task ProcessScheduledAlertsAsync(
        IOrganizationRepository organizationRepository,
        IEmailRepository emailRepository,
        IReservationRepository reservationRepository,
        IEmailManager emailManager,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var alerts = await LoadAllAlertsAsync(organizationRepository, emailRepository, cancellationToken);

        foreach (var alert in alerts)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (!alert.IsActive)
                continue;

            await HydrateDepartureDateIfNeededAsync(alert, reservationRepository);

            if (!AlertScheduleEvaluator.IsDue(alert, utcNow))
                continue;

            try
            {
                var organization = await organizationRepository.GetOrganizationByIdAsync(alert.OrganizationId);
                if (organization == null || !organization.IsActive)
                    continue;

                var email = MapAlertToEmail(alert);
                var result = await emailManager.SendEmail(organization.SendGridName, email);

                ApplySendResultToAlert(alert, result);

                var shouldDeactivateAlert = alert.Frequency == FrequencyType.OneTime
                    && (result.EmailStatus == EmailStatus.Succeeded || result.SentOn.HasValue);
                if (shouldDeactivateAlert)
                    alert.IsActive = false;

                await emailRepository.UpdateAlertEmailStatusAsync(alert);

                if (result.EmailStatus == EmailStatus.Succeeded)
                    _logger.LogInformation(
                        "Scheduled alert email sent. AlertId={AlertId}, OrganizationId={OrganizationId}",
                        alert.AlertId,
                        alert.OrganizationId);
                else
                    _logger.LogWarning(
                        "Scheduled alert email did not succeed. AlertId={AlertId}, Status={Status}, LastError={LastError}",
                        alert.AlertId,
                        result.EmailStatus,
                        result.LastError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alert {AlertId}", alert.AlertId);
            }
        }
    }

    private static async Task<List<Alert>> LoadAllAlertsAsync(IOrganizationRepository organizationRepository, IEmailRepository emailRepository, CancellationToken cancellationToken)
    {
        var all = new List<Alert>();
        var organizations = await organizationRepository.GetOrganizationsAsync();

        foreach (var org in organizations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!org.IsActive)
                continue;

            var offices = await organizationRepository.GetOfficesByOrganizationIdAsync(org.OrganizationId);
            var officeList = offices.ToList();
            if (officeList.Count == 0)
                continue;

            var officeCsv = string.Join(",", officeList.Select(o => o.OfficeId));
            var batch = await emailRepository.GetActiveAlertsByOfficeIdsAsync(org.OrganizationId, officeCsv);
            all.AddRange(batch);
        }

        return all;
    }

    private static Email MapAlertToEmail(Alert alert)
    {
        return new Email
        {
            OrganizationId = alert.OrganizationId,
            OfficeId = alert.OfficeId,
            PropertyId = alert.PropertyId,
            ReservationId = alert.ReservationId,
            FromRecipient = EmailAddress.Create(alert.FromRecipient.Email, alert.FromRecipient.Name),
            ToRecipients = alert.ToRecipients
                .Select(r => EmailAddress.Create(r.Email, r.Name))
                .ToList(),
            CcRecipients = alert.CcRecipients
                .Select(r => EmailAddress.Create(r.Email, r.Name))
                .ToList(),
            BccRecipients = alert.BccRecipients
                .Select(r => EmailAddress.Create(r.Email, r.Name))
                .ToList(),
            Subject = alert.Subject,
            PlainTextContent = alert.PlainTextContent,
            HtmlContent = string.Empty,
            EmailType = alert.EmailType,
            EmailStatus = EmailStatus.Attempting,
            CreatedBy = alert.CreatedBy != Guid.Empty ? alert.CreatedBy : Guid.Empty
        };
    }

    private static void ApplySendResultToAlert(Alert alert, Email email)
    {
        alert.EmailStatus = email.EmailStatus;
        alert.AttemptCount = email.AttemptCount;
        alert.LastError = email.LastError;
        alert.LastAttemptedOn = email.LastAttemptedOn;
        alert.SentOn = email.SentOn;
        alert.ModifiedBy = alert.CreatedBy != Guid.Empty ? alert.CreatedBy : Guid.Empty;
    }

    private async Task HydrateDepartureDateIfNeededAsync(Alert alert, IReservationRepository reservationRepository)
    {
        if (!alert.DaysBeforeDeparture.HasValue || alert.DepartureDate.HasValue || !alert.ReservationId.HasValue)
            return;

        try
        {
            var reservation = await reservationRepository.GetReservationByIdAsync(alert.ReservationId.Value, alert.OrganizationId);
            if (reservation != null)
                alert.DepartureDate = reservation.DepartureDate;
            else
                _logger.LogWarning(
                    "Alert has DaysBeforeDeparture but reservation was not found. AlertId={AlertId}, ReservationId={ReservationId}",
                    alert.AlertId,
                    alert.ReservationId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to hydrate departure date for alert. AlertId={AlertId}, ReservationId={ReservationId}",
                alert.AlertId,
                alert.ReservationId.Value);
        }
    }
    #endregion

    #region Retire Links
    private async Task ProcessRetireExpiredListingLinksAsync(IPropertyRepository propertyRepository, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await propertyRepository.DeleteExpiredPropertyListingSharesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Property listing share delete-expired job failed");
        }
    }

    private async Task ProcessRetireExpiredOwnerFormLinksAsync(ILeadRepository leadRepository, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await leadRepository.DeleteExpiredOwnerFormSharesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Owner form share delete-expired job failed");
        }
    }

    private async Task ProcessLogRetentionAsync(ILoggingRepository loggingRepository, CancellationToken cancellationToken)
    {
        if (!_applicationLoggingSettings.RetentionEnabled)
            return;

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        if (_lastRetentionRunDateUtc == todayUtc)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var retainDays = Math.Max(1, _applicationLoggingSettings.RetentionDays);
            await loggingRepository.ApplyLogRetentionAsync(retainDays);
            _lastRetentionRunDateUtc = todayUtc;
            _logger.LogInformation("Logging retention applied for {RetainDays} days.", retainDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logging retention job failed");
        }
    }
    #endregion
}
