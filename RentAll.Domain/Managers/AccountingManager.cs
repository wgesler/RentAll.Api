using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public class AccountingManager : IAccountingManager
{
	const int PRORATE_DAYS = 30;
	private readonly IInvoiceRepository _invoiceRepository;
	private readonly ICostCodeRepository _costCodeRepository;
	private readonly IReservationRepository _reservationRepository;

	public AccountingManager(IInvoiceRepository invoiceRepository, ICostCodeRepository costCodeRepository, IReservationRepository reservationRepository)
	{
		_invoiceRepository = invoiceRepository;
		_costCodeRepository = costCodeRepository;
		_reservationRepository = reservationRepository;
	}

	public async Task<Reservation> ApplyInvoiceToReservationAsync(Invoice i)
	{
		if (i.ReservationId == null)
			throw new Exception("Invoice missing ReservationId");

		return await _reservationRepository.IncrementCurrentInvoiceAsync((Guid)i.ReservationId, i.OrganizationId);
	}

	public async Task ApplyPaymentToReservationAsync(Guid reservationId, Guid organizationId, string offices, int costCodeId, 
		string description, decimal amountPaid, Guid currentUser)
	{
		var reservation = await _reservationRepository.GetByIdAsync(reservationId, organizationId);
		if (reservation == null)
			return;

		// Account for previous credits on this account
		var availableAmount = amountPaid + reservation.CreditDue;

		var invoices = (await _invoiceRepository.GetAllByReservationIdAsync(reservationId, organizationId, offices))
			.Where(i => i.IsActive).OrderBy(i => i.InvoiceDate).ToList();
		
		foreach (var invoice in invoices)
		{
			if (availableAmount <= 0)
				break;

			// Skip over already paid invoices
			var remainingBalance = invoice.TotalAmount - invoice.PaidAmount;
			if (remainingBalance <= 0)
				continue; 

			if (availableAmount >= remainingBalance)
			{
				// Full payment for this invoice
				invoice.PaidAmount = invoice.TotalAmount;
				availableAmount -= remainingBalance;
				invoice.LedgerLines.Add(new LedgerLine { InvoiceId = invoice.InvoiceId, ReservationId = reservationId, CostCodeId = costCodeId, Description = description, Amount = amountPaid, CreatedBy = currentUser });
				await _invoiceRepository.UpdateByIdAsync(invoice);
			}
			else
			{
				// Partial payment
				invoice.PaidAmount += availableAmount;
				availableAmount = 0;
				invoice.LedgerLines.Add(new LedgerLine { InvoiceId = invoice.InvoiceId, ReservationId = reservationId, CostCodeId = costCodeId, Description = description, Amount = amountPaid, CreatedBy = currentUser });
				await _invoiceRepository.UpdateByIdAsync(invoice);
			}
		}

		// If we still have remaining funds, add a credit to the reservation
		if (availableAmount > 0) {
			reservation.CreditDue = availableAmount;
			await _reservationRepository.UpdateByIdAsync(reservation);
		}
	}

	public async Task<List<LedgerLine>> CreateLedgerLinesForReservationIdAsync(Reservation reservation, DateTimeOffset startDate, DateTimeOffset endDate)
	{
		var ledgerLines = GetLedgerLinesByReservationIdAsync(reservation, startDate, endDate);
		await ApplyCostCodesAsync(reservation.OfficeId, reservation.OrganizationId, ledgerLines);

		return ledgerLines;
	}

	public List<LedgerLine> GetLedgerLinesByReservationIdAsync(Reservation reservation, DateTimeOffset startDate, DateTimeOffset endDate)
	{
		var lineItems = new List<LedgerLine>();

		var requestedDate = startDate.Date;
		var requestedMonth = startDate.Month;

		var checkInDate = reservation.ArrivalDate.Date;
		var checkInMonth = reservation.ArrivalDate.Month;
		var checkOutDate = reservation.DepartureDate.Date;
		var checkOutMonth = reservation.DepartureDate.Month;

		// Determine if we have a partial month scenario
		// Partial month = NO if: check-in is 1st, OR (month has 31 days AND check-in is 1st or 2nd)
		var daysInCheckInMonth = DateTime.DaysInMonth(checkInDate.Year, checkInDate.Month);
		var isFirstMonthPartial = !(checkInDate.Day == 1 || (daysInCheckInMonth == 31 && checkInDate.Day <= 2));

		DateTime firstDayOfMonth = new DateTime(requestedDate.Year, requestedDate.Month, 1);
		DateTime lastDayOfMonth = new DateTime(requestedDate.Year, requestedDate.Month, DateTime.DaysInMonth(requestedDate.Year, requestedDate.Month));
		DateTime firstDayOfCheckInMonth = new DateTime(checkInDate.Year, checkInDate.Month, 1);
		DateTime lastDayOfCheckInMonth = new DateTime(checkInDate.Year, checkInDate.Month, DateTime.DaysInMonth(checkInDate.Year, checkInDate.Month));

		// Calculate secondMonth based on whether first month was prorated or not
		// If prorated: billed to end of first month, so second month starts the day after
		// If not prorated: billed for 30 days from check-in
		var secondMonthDate = (reservation.ProrateType == ProrateType.FirstMonth) ? lastDayOfCheckInMonth.AddDays(1) : checkInDate.AddDays(PRORATE_DAYS + 1);
		var secondMonth = secondMonthDate.Month;

		var isFirstMonth = requestedMonth == checkInMonth;
		var isProratedMonth = (requestedMonth == checkInMonth && reservation.ProrateType == ProrateType.FirstMonth) ||
							  (requestedMonth == secondMonth && reservation.ProrateType == ProrateType.SecondMonth);

		// Get any first month lines
		if (isFirstMonth)
			GetFirstMonthLines(reservation, isFirstMonth, lineItems);

		// If you're in and out in the same month
		if (checkInMonth == requestedMonth && checkOutMonth == requestedMonth)
		{
			var days = CalculateNumberOfDays(checkInDate, checkOutDate, reservation.BillingType);
			AddRentalLine(days, reservation, checkInDate, checkOutDate, isProratedMonth, lineItems);
			AddMaidServiceLines(reservation, requestedDate.Year, requestedMonth, lineItems);
			return lineItems;
		}

		// If this is your first month (only process special logic if partial month)
		if (requestedMonth == checkInMonth && isFirstMonthPartial)
		{
			var lastDay = isProratedMonth ? lastDayOfMonth : checkInDate.AddDays(PRORATE_DAYS);
			var days = CalculateNumberOfDays(checkInDate, lastDay, reservation.BillingType);
			AddRentalLine(days, reservation, checkInDate, lastDay, isProratedMonth, lineItems);
			AddMaidServiceLines(reservation, requestedDate.Year, requestedMonth, lineItems);
			return lineItems;
		}

		// If this is your second month (only process special logic if partial month)
		if (requestedMonth == secondMonth && isFirstMonthPartial)
		{
			var firstDay = isProratedMonth ? checkInDate.AddDays(PRORATE_DAYS + 1) : firstDayOfMonth;
			var days = CalculateNumberOfDays(firstDay, lastDayOfMonth, reservation.BillingType);
			AddRentalLine(days, reservation, firstDay, lastDayOfMonth, isProratedMonth, lineItems);
			AddMaidServiceLines(reservation, requestedDate.Year, requestedMonth, lineItems);
			return lineItems;
		}

		// Otherwise, simply bill for the entire month
		var checkoutDays = CalculateNumberOfDays(firstDayOfMonth, lastDayOfMonth, reservation.BillingType);
		AddRentalLine(checkoutDays, reservation, firstDayOfMonth, lastDayOfMonth, isProratedMonth, lineItems);
		AddMaidServiceLines(reservation, requestedDate.Year, requestedMonth, lineItems);
		return lineItems;
	}

	public async Task ApplyCostCodesAsync(int officeId, Guid organizationId, List<LedgerLine> ledgerLines)
	{
		var costCodes = await _costCodeRepository.GetAllByOfficeIdAsync(officeId, organizationId);
		foreach (var line in ledgerLines)
		{
			var costCode = null as CostCode;
			switch (line.Description)
			{
				case string desc when desc.StartsWith("Security Deposit Waiver", StringComparison.OrdinalIgnoreCase):
					costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Security Deposit Waiver"));
					if (costCode != null) line.CostCodeId = costCode.CostCodeId;
					continue;
				case string desc when desc.StartsWith("Deposit", StringComparison.OrdinalIgnoreCase):
					costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Deposit"));
					if (costCode != null) line.CostCodeId = costCode.CostCodeId;
					continue;
				case string desc when desc.StartsWith("Rental Fee", StringComparison.OrdinalIgnoreCase):
					costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Rent Property"));
					if (costCode != null) line.CostCodeId = costCode.CostCodeId;
					continue;
				case string desc when desc.StartsWith("Maid Service", StringComparison.OrdinalIgnoreCase):
					costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Maid Service"));
					if (costCode != null) line.CostCodeId = costCode.CostCodeId;
					continue;
				case string desc when desc.StartsWith("Pet Fee", StringComparison.OrdinalIgnoreCase):
					costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Pet Fee"));
					if (costCode != null) line.CostCodeId = costCode.CostCodeId;
					continue;
				case string desc when desc.StartsWith("Departure Fee", StringComparison.OrdinalIgnoreCase):
					costCode = costCodes.FirstOrDefault(cc => cc.Description.Contains("Departure Fee"));
					if (costCode != null) line.CostCodeId = costCode.CostCodeId;
					continue;
			}

		}
	}

	#region Private Methods
	private void GetFirstMonthLines(Reservation reservation, bool isFirstMonth, List<LedgerLine> lines)
	{
		if (reservation.DepositType == DepositType.Deposit)
			lines.Add(new LedgerLine { Description = "Deposit", Amount = reservation.Deposit });
		if (reservation.DepositType == DepositType.SDW)
			lines.Add(new LedgerLine { Description = "Security Deposit Waiver", Amount = reservation.Deposit });
		if (reservation.HasPets)
			lines.Add(new LedgerLine { Description = "Pet Fee", Amount = reservation.PetFee });
		if (reservation.DepartureFee >= 0)
			lines.Add(new LedgerLine { Description = "Departure Fee", Amount = reservation.DepartureFee });
	}

	private void AddRentalLine(int days, Reservation reservation, DateTimeOffset startDate, DateTimeOffset endDate, bool isProratedMonth, List<LedgerLine> lines)
	{
		var rentLine = $"Rental Fee ({startDate.LocalDateTime:MM/dd}-{endDate.LocalDateTime:MM/dd})";
		if (reservation.BillingType == BillingType.Monthly)
		{
			if (isProratedMonth)
				lines.Add(new LedgerLine { Description = rentLine, Amount = (reservation.BillingRate / PRORATE_DAYS) * days });
			else
				lines.Add(new LedgerLine { Description = rentLine, Amount = reservation.BillingRate });
		}
		else
			lines.Add(new LedgerLine { Description = rentLine, Amount = days * reservation.BillingRate });
	}

	private void AddMaidServiceLines(Reservation reservation, int requestedYear, int requestedMonth, List<LedgerLine> lines)
	{
		var startDate = reservation.MaidStartDate;

		int maidServices = 0;
		switch (reservation.Frequency)
		{
			case FrequencyType.Weekly:
				maidServices = CountNumberOfWeekDaysInMonth(startDate, reservation.DepartureDate, requestedYear, requestedMonth);
				break;
			case FrequencyType.EOW:
				maidServices = CountEowDaysInMonth(startDate, reservation.DepartureDate, requestedYear, requestedMonth);
				break;
			default:
				maidServices = CountNumberOfMonths(startDate, reservation.DepartureDate, requestedYear, requestedMonth, reservation.Frequency);
				break;
		}

		if (maidServices > 0)
			lines.Add(new LedgerLine { Description = $"Maid Service ({maidServices} times)", Amount = maidServices * reservation.MaidServiceFee });
	}
	#endregion 

	#region Day Calculation Methods
	private static int CalculateNumberOfDays(DateTimeOffset startDate, DateTimeOffset endDate, BillingType billingType)
	{
		DateTime start = startDate.Date;
		DateTime end = endDate.Date;
		if (end < start) return 0;

		var days = (end - start).Days;
		if (billingType != BillingType.Nightly)
			days++;
		return days;
	}

	private static int CountNumberOfWeekDaysInMonth(DateTimeOffset sDate, DateTimeOffset dDate, int currentYear, int currentMonth)
	{
		DateTimeOffset startDate = sDate.Date;
		DateTimeOffset stopDate = dDate.Date.AddDays(-7); // 7 days prior to departureDate
		DayOfWeek targetDayOfWeek = startDate.DayOfWeek;

		DateTimeOffset firstDayOfMonth = new DateTime(currentYear, currentMonth, 1);
		DateTimeOffset lastDayOfMonth = new DateTime(currentYear, currentMonth, DateTime.DaysInMonth(currentYear, currentMonth));

		int count = 0;
		for (DateTimeOffset d = startDate; d <= stopDate; d = d.AddDays(7))
		{
			if (d.Year > currentYear || (d.Year == currentYear && d.Month > currentMonth))
				break;

			// Check if date is in current month/year AND after startDate AND before stopDate
			if (d.Year == currentYear && d.Month == currentMonth && d >= startDate && d <= stopDate)
				count++;
		}

		return count;
	}

	private static int CountEowDaysInMonth(DateTimeOffset sDate, DateTimeOffset dDate, int currentYear, int currentMonth)
	{
		DateTimeOffset startDate = sDate.Date;
		DateTimeOffset stopDate = dDate.Date.AddDays(-7); // 7 days prior to departureDate
		DayOfWeek targetDayOfWeek = startDate.DayOfWeek;

		DateTimeOffset firstDayOfMonth = new DateTime(currentYear, currentMonth, 1);
		DateTimeOffset lastDayOfMonth = new DateTime(currentYear, currentMonth, DateTime.DaysInMonth(currentYear, currentMonth));

		int count = 0;
		for (DateTimeOffset d = startDate; d <= stopDate; d = d.AddDays(14))
		{
			if (d.Year > currentYear || (d.Year == currentYear && d.Month > currentMonth))
				break;

			// Check if date is in current month/year AND after startDate AND before stopDate
			if (d.Year == currentYear && d.Month == currentMonth && d >= startDate && d <= stopDate)
				count++;
		}

		return count;
	}

	private static int CountNumberOfMonths(DateTimeOffset sDate, DateTimeOffset dDate, int currentYear, int currentMonth, FrequencyType frequency)
	{
		DateTimeOffset startDate = sDate.Date;
		DateTimeOffset stopDate = dDate.Date.AddDays(-7); // 7 days prior to departureDate

		DateTimeOffset firstDayOfMonth = new DateTime(currentYear, currentMonth, 1);
		DateTimeOffset lastDayOfMonth = new DateTime(currentYear, currentMonth, DateTime.DaysInMonth(currentYear, currentMonth));

		// Determine the interval based on frequency
		int monthInterval;
		switch (frequency)
		{
			case FrequencyType.Monthly:
				monthInterval = 1;
				break;
			case FrequencyType.Quarterly:
				monthInterval = 3; 
				break;
			case FrequencyType.BiAnnually:
				monthInterval = 6;
				break;
			case FrequencyType.Annually:
				monthInterval = 12;
				break;
			default:
				return 0;
		}

		int count = 0;
		for (DateTimeOffset d = startDate; d <= stopDate; d = d.AddMonths(monthInterval))
		{
			// If d > current month/year, break
			if (d.Year > currentYear || (d.Year == currentYear && d.Month > currentMonth))
				break;

			// Check if date is in current month/year AND after startDate AND before stopDate
			if (d.Year == currentYear && d.Month == currentMonth && d >= startDate && d <= stopDate)
				count++;
		}

		return count;
	}
	#endregion
}
