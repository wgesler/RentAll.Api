-- =============================================
-- ExtraFeeLine CRUD Stored Procedures
-- =============================================

-- =============================================
-- Property.ExtraFeeLine_Add
-- =============================================
-- Description: Creates a new ExtraFeeLine
-- Parameters:
--   @ReservationId UNIQUEIDENTIFIER - The reservation ID
--   @FeeDescription NVARCHAR(MAX) - Description of the fee
--   @FeeAmount DECIMAL(18,2) - Amount of the fee
--   @FeeFrequencyId INT - Frequency type ID (maps to FrequencyType enum)
-- Returns: ExtraFeeLineEntity with the created record
-- =============================================
/*
CREATE PROCEDURE Property.ExtraFeeLine_Add
	@ReservationId UNIQUEIDENTIFIER,
	@FeeDescription NVARCHAR(MAX),
	@FeeAmount DECIMAL(18,2),
	@FeeFrequencyId INT
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO Property.ExtraFeeLine (ReservationId, FeeDescription, FeeAmount, FeeFrequencyId)
	VALUES (@ReservationId, @FeeDescription, @FeeAmount, @FeeFrequencyId);

	SELECT 
		ExtraFeeLineId,
		ReservationId,
		FeeDescription,
		FeeAmount,
		FeeFrequencyId
	FROM Property.ExtraFeeLine
	WHERE ExtraFeeLineId = SCOPE_IDENTITY();
END
GO
*/

-- =============================================
-- Property.ExtraFeeLine_GetById
-- =============================================
-- Description: Gets an ExtraFeeLine by ID
-- Parameters:
--   @ExtraFeeLineId INT - The ExtraFeeLine ID
-- Returns: ExtraFeeLineEntity
-- =============================================
/*
CREATE PROCEDURE Property.ExtraFeeLine_GetById
	@ExtraFeeLineId INT
AS
BEGIN
	SET NOCOUNT ON;

	SELECT 
		ExtraFeeLineId,
		ReservationId,
		FeeDescription,
		FeeAmount,
		FeeFrequencyId
	FROM Property.ExtraFeeLine
	WHERE ExtraFeeLineId = @ExtraFeeLineId;
END
GO
*/

-- =============================================
-- Property.ExtraFeeLine_GetAllByReservationId
-- =============================================
-- Description: Gets all ExtraFeeLines for a reservation
-- Parameters:
--   @ReservationId UNIQUEIDENTIFIER - The reservation ID
-- Returns: List of ExtraFeeLineEntity
-- =============================================
/*
CREATE PROCEDURE Property.ExtraFeeLine_GetAllByReservationId
	@ReservationId UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;

	SELECT 
		ExtraFeeLineId,
		ReservationId,
		FeeDescription,
		FeeAmount,
		FeeFrequencyId
	FROM Property.ExtraFeeLine
	WHERE ReservationId = @ReservationId
	ORDER BY ExtraFeeLineId;
END
GO
*/

-- =============================================
-- Property.ExtraFeeLine_UpdateById
-- =============================================
-- Description: Updates an existing ExtraFeeLine
-- Parameters:
--   @ExtraFeeLineId INT - The ExtraFeeLine ID
--   @ReservationId UNIQUEIDENTIFIER - The reservation ID
--   @FeeDescription NVARCHAR(MAX) - Description of the fee
--   @FeeAmount DECIMAL(18,2) - Amount of the fee
--   @FeeFrequencyId INT - Frequency type ID (maps to FrequencyType enum)
-- Returns: ExtraFeeLineEntity with the updated record
-- =============================================
/*
CREATE PROCEDURE Property.ExtraFeeLine_UpdateById
	@ExtraFeeLineId INT,
	@ReservationId UNIQUEIDENTIFIER,
	@FeeDescription NVARCHAR(MAX),
	@FeeAmount DECIMAL(18,2),
	@FeeFrequencyId INT
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE Property.ExtraFeeLine
	SET 
		ReservationId = @ReservationId,
		FeeDescription = @FeeDescription,
		FeeAmount = @FeeAmount,
		FeeFrequencyId = @FeeFrequencyId
	WHERE ExtraFeeLineId = @ExtraFeeLineId;

	SELECT 
		ExtraFeeLineId,
		ReservationId,
		FeeDescription,
		FeeAmount,
		FeeFrequencyId
	FROM Property.ExtraFeeLine
	WHERE ExtraFeeLineId = @ExtraFeeLineId;
END
GO
*/

-- =============================================
-- Property.ExtraFeeLine_DeleteById
-- =============================================
-- Description: Deletes an ExtraFeeLine by ID
-- Parameters:
--   @ExtraFeeLineId INT - The ExtraFeeLine ID
-- Returns: None
-- =============================================
/*
CREATE PROCEDURE Property.ExtraFeeLine_DeleteById
	@ExtraFeeLineId INT
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM Property.ExtraFeeLine
	WHERE ExtraFeeLineId = @ExtraFeeLineId;
END
GO
*/
