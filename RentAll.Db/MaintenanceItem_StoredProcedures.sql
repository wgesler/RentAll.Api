-- =============================================
-- MaintenanceItem Table + CRUD Stored Procedures
-- =============================================

-- =============================================
-- Maintenance.MaintenanceItem table
-- =============================================
/*
CREATE TABLE [Maintenance].[MaintenanceItem] (
	[MaintenanceItemId]			INT IDENTITY (1, 1) NOT NULL,
	[PropertyId]				UNIQUEIDENTIFIER	NOT NULL,
	[Name]						VARCHAR (250)		NOT NULL,
	[Notes]						VARCHAR (2500)		NULL,
	[MonthsBetweenService]		INT					DEFAULT ((12)) NOT NULL,
	[LastServicedOn]			DATETIMEOFFSET		NULL,
	CONSTRAINT [PK_MaintenanceItem] PRIMARY KEY CLUSTERED ([MaintenanceItemId] ASC)
);
GO
*/

-- =============================================
-- Maintenance.MaintenanceItem_Add
-- =============================================
/*
CREATE PROCEDURE [Maintenance].[MaintenanceItem_Add]
	@PropertyId UNIQUEIDENTIFIER,
	@Name VARCHAR(250),
	@Notes VARCHAR(2500) = NULL,
	@MonthsBetweenService INT = 12,
	@LastServicedOn DATETIMEOFFSET = NULL
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO [Maintenance].[MaintenanceItem]
	(
		PropertyId,
		Name,
		Notes,
		MonthsBetweenService,
		LastServicedOn
	)
	VALUES
	(
		@PropertyId,
		@Name,
		@Notes,
		@MonthsBetweenService,
		@LastServicedOn
	);

	SELECT
		MaintenanceItemId,
		PropertyId,
		Name,
		Notes,
		MonthsBetweenService,
		LastServicedOn
	FROM [Maintenance].[MaintenanceItem]
	WHERE PropertyId = @PropertyId AND Name = @Name;
END
GO
*/

-- =============================================
-- Maintenance.MaintenanceItem_GetByPropertyId
-- =============================================
/*
CREATE PROCEDURE [Maintenance].[MaintenanceItem_GetByPropertyId]
	@PropertyId UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		MaintenanceItemId,
		PropertyId,
		Name,
		Notes,
		MonthsBetweenService,
		LastServicedOn
	FROM [Maintenance].[MaintenanceItem]
	WHERE PropertyId = @PropertyId
	ORDER BY Name;
END
GO
*/

-- =============================================
-- Maintenance.MaintenanceItem_GetById
-- =============================================
/*
CREATE PROCEDURE [Maintenance].[MaintenanceItem_GetById]
	@MaintenanceItemId INT
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		MaintenanceItemId,
		PropertyId,
		Name,
		Notes,
		MonthsBetweenService,
		LastServicedOn
	FROM [Maintenance].[MaintenanceItem]
	WHERE MaintenanceItemId = @MaintenanceItemId;
END
GO
*/

-- =============================================
-- Maintenance.MaintenanceItem_UpdateById
-- =============================================
/*
CREATE PROCEDURE [Maintenance].[MaintenanceItem_UpdateById]
	@MaintenanceItemId INT,
	@PropertyId UNIQUEIDENTIFIER,
	@Name VARCHAR(250),
	@Notes VARCHAR(2500) = NULL,
	@MonthsBetweenService INT,
	@LastServicedOn DATETIMEOFFSET = NULL
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE [Maintenance].[MaintenanceItem]
	SET
		PropertyId = @PropertyId,
		Name = @Name,
		Notes = @Notes,
		MonthsBetweenService = @MonthsBetweenService,
		LastServicedOn = @LastServicedOn
	WHERE MaintenanceItemId = @MaintenanceItemId;

	SELECT
		MaintenanceItemId,
		PropertyId,
		Name,
		Notes,
		MonthsBetweenService,
		LastServicedOn
	FROM [Maintenance].[MaintenanceItem]
	WHERE MaintenanceItemId = @MaintenanceItemId;
END
GO
*/

-- =============================================
-- Maintenance.MaintenanceItem_DeleteById
-- =============================================
/*
CREATE PROCEDURE [Maintenance].[MaintenanceItem_DeleteById]
	@MaintenanceItemId INT
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM [Maintenance].[MaintenanceItem]
	WHERE MaintenanceItemId = @MaintenanceItemId;
END
GO
*/
