using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public class AccountingManager : IAccountingManager
{
	const int PRORATE_DAYS = 30;
	private readonly IInvoiceRepository _invoiceRepository;
	private readonly IReservationRepository _reservationRepository;

	public AccountingManager(IInvoiceRepository invoiceRepository, IReservationRepository reservationRepository)
	{
		_invoiceRepository = invoiceRepository;
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

	public List<LedgerLine> GetLedgerLinesByReservationIdAsync(Reservation reservation, DateTimeOffset startDate, DateTimeOffset endDate)
	{
		var lineItems = new List<LedgerLine>();

		var requestedDate = startDate.Date;
		var requestedMonth = startDate.Month;

		var checkInDate = reservation.ArrivalDate.Date;
		var checkInMonth = reservation.ArrivalDate.Month;
		var checkOutDate = reservation.DepartureDate.Date;
		var checkOutMonth = reservation.DepartureDate.Month;
		var firstMonth = reservation.ArrivalDate.Month;
		var secondMonth = reservation.ArrivalDate.Month + 1;

		DateTime firstDayOfMonth = new DateTime(requestedDate.Year, requestedDate.Month, 1);
		DateTime lastDayOfMonth = new DateTime(requestedDate.Year, requestedDate.Month, DateTime.DaysInMonth(requestedDate.Year, requestedDate.Month));
		DateTime firstDayOfCheckInMonth = new DateTime(checkInDate.Year, checkInDate.Month, 1);
		DateTime lastDayOfCheckInMonth = new DateTime(checkInDate.Year, checkInDate.Month, DateTime.DaysInMonth(checkInDate.Year, checkInDate.Month));

		var isFirstMonth = checkInMonth == firstMonth;
		var isProratedMonth = (checkInMonth == firstMonth && reservation.ProrateType == ProrateType.FirstMonth ) ||
							  (checkInMonth == secondMonth && reservation.ProrateType == ProrateType.SecondMonth);

		// Get any first month lines
		GetFirstMonthLines(reservation, isFirstMonth, lineItems);

		// If you're in and out in the same month
		if (checkInMonth == requestedMonth && checkOutMonth == requestedMonth)
		{
			var days = CalculateNumberOfDays(checkInDate, checkOutDate, reservation.BillingType);
			AddRentalLine(days, reservation, checkInDate, checkOutDate, lineItems);
			AddMaidServiceLines(reservation, lineItems);
			return lineItems;
		}

		// If this is your first month
		if (checkInMonth == requestedMonth)
		{	
			var days = CalculateNumberOfDays(checkInDate, lastDayOfMonth, reservation.BillingType);	
			var totalDays = isProratedMonth ? days : PRORATE_DAYS; 
			var lastDate = isProratedMonth ? lastDayOfMonth : checkInDate.AddDays(PRORATE_DAYS - days);
			AddRentalLine(totalDays, reservation, checkInDate, lastDate, lineItems);
			AddMaidServiceLines(reservation, lineItems);
			return lineItems;
		}

		// If this is your second month
		if (requestedMonth == secondMonth)
		{
			var days = CalculateNumberOfDays(firstDayOfMonth, lastDayOfMonth, reservation.BillingType);
			var daysInFirstMonth = CalculateNumberOfDays(firstDayOfCheckInMonth, lastDayOfCheckInMonth, reservation.BillingType);
			var prepaidDays = PRORATE_DAYS - daysInFirstMonth;
			var totalDays = isProratedMonth ? days - prepaidDays : days; 
			var firstDate = isProratedMonth ? firstDayOfMonth : firstDayOfMonth.AddDays(prepaidDays);
			AddRentalLine(totalDays, reservation, firstDate, lastDayOfMonth, lineItems);
			AddMaidServiceLines(reservation, lineItems);
			return lineItems;
		}

		// Otherwise, simply bill for the entire month
		var checkoutDays = CalculateNumberOfDays(firstDayOfMonth, lastDayOfCheckInMonth, reservation.BillingType);
		AddRentalLine(checkoutDays, reservation, firstDayOfMonth, lastDayOfCheckInMonth, lineItems);
		AddMaidServiceLines(reservation, lineItems);
		return lineItems;
	}


	#region Private Methods
	private void GetFirstMonthLines(Reservation reservation, bool isFirstMonth, List<LedgerLine> lines)
	{
		if (!isFirstMonth)
			return;

		if (reservation.DepositType == DepositType.Deposit)
			lines.Add(new LedgerLine { Description = "Deposit", Amount = reservation.Deposit });
		if (reservation.DepositType == DepositType.SDW)
			lines.Add(new LedgerLine { Description = "Security Deposit Waiver", Amount = reservation.Deposit });
		if (reservation.HasPets)
			lines.Add(new LedgerLine { Description = "Pet Fee", Amount = reservation.PetFee });
		if (reservation.DepartureFee >= 0)
			lines.Add(new LedgerLine { Description = "Departure Fee", Amount = reservation.DepartureFee });
	}

	private void AddRentalLine(int days, Reservation reservation, DateTimeOffset startDate, DateTimeOffset endDate, List<LedgerLine> lines)
	{
		var rentLine = $"Rental Fee ({startDate.LocalDateTime:MM/dd}-{endDate.LocalDateTime:MM/dd})";
		if (reservation.BillingType == BillingType.Monthly)
			lines.Add(new LedgerLine { Description = rentLine, Amount = reservation.BillingRate });
		else
			lines.Add(new LedgerLine { Description = rentLine, Amount = days * reservation.BillingRate });
	}

	private void AddMaidServiceLines(Reservation reservation, List<LedgerLine> lines)
	{
		var startDate = reservation.MaidStartDate;
		var currentMonth = DateTime.UtcNow.Month;
		var currentYear = DateTime.UtcNow.Year;

		int maidServices = 0;
		switch (reservation.Frequency)
		{
			case FrequencyType.Weekly:
				maidServices = CountNumberOfWeekDaysInMonth(startDate, reservation.DepartureDate, currentYear, currentMonth);
				break;
			case FrequencyType.EOW:
				maidServices = CountEowDaysInMonth(startDate, reservation.DepartureDate, currentYear, currentMonth);
				break;
			default:
				maidServices = CountNumberOfMonths(startDate, reservation.DepartureDate, currentYear, currentMonth, reservation.Frequency);
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
