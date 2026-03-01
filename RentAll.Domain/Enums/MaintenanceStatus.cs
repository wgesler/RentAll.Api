namespace RentAll.Domain.Enums;

public enum MaintenanceStatus
{
    UnProcessed = 0,
    NeedToScheduleCleaners = 1,
    CleanersScheduled = 2,
    Cleaned = 3,
    WaitingForInspection = 4,
    InspectedWithIssues = 5,
    NeedToScheduleMaintenance = 6,
    MaintenanceScheduled = 7,
    MaintenanceComplete = 8,
    InspectionComplete = 9,
    Ready = 10
}
