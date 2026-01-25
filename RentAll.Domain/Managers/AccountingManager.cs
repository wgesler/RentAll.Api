using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using System.Linq;
using System.Threading.Tasks;

namespace RentAll.Domain.Managers;

public class AccountingManager : IAccountingManager
{
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

	public async Task ApplyPaymentToReservationAsync(Guid reservationId, Guid organizationId, string offices, decimal amountPaid)
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
				await _invoiceRepository.UpdateByIdAsync(invoice);
			}
			else
			{
				// Partial payment
				invoice.PaidAmount += availableAmount;
				availableAmount = 0;
				await _invoiceRepository.UpdateByIdAsync(invoice);
			}
		}

		// If we still have remaining funds, add a credit to the reservation
		if (availableAmount > 0) 
			reservation.CreditDue = availableAmount;
	}

	public List<LedgerLine> GetLedgerLinesByReservationIdAsync(Reservation reservation)
	{
		var lineItems = new List<LedgerLine>();

		var currentDate = DateTime.UtcNow.Date;
		var currentMonth = DateTime.UtcNow.Month;
		var checkInDate = reservation.ArrivalDate.Date;
		var checkInMonth = reservation.ArrivalDate.Month;
		var checkOutDate = reservation.DepartureDate.Date;
		var checkOutMonth = reservation.DepartureDate.Month;

		// If your stay is less than a month, you get all charges
		if (checkInMonth == currentMonth || checkOutMonth == currentMonth)
		{
			var days = CalculateNumberOfDays(checkInDate, checkOutDate, reservation.BillingType);
			GetFirstMonthLines(reservation, lineItems);
			AddRentalLine(days, reservation, lineItems);
			AddMaidServiceLines(reservation, lineItems);
			GetLastMonthLines(reservation, lineItems);
		}
		// If this is your first month
		else if (checkInMonth == currentMonth)
		{
			DateTime lastDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
			var days = CalculateNumberOfDays(checkInDate, lastDayOfMonth, reservation.BillingType);
			GetFirstMonthLines(reservation, lineItems);
			AddRentalLine(days, reservation, lineItems);
			AddMaidServiceLines(reservation, lineItems);
		}
		// If this is your last month
		else if (checkOutMonth == currentMonth)
		{
			DateTime firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
			var days = CalculateNumberOfDays(firstDayOfMonth, checkOutDate, reservation.BillingType);
			AddRentalLine(days, reservation, lineItems);
			AddMaidServiceLines(reservation, lineItems);
			GetLastMonthLines(reservation, lineItems);
		}
		// This is a mid-month stay
		else
		{
			DateTime firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
			DateTime lastDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
			var days = CalculateNumberOfDays(firstDayOfMonth, lastDayOfMonth, reservation.BillingType);
			AddRentalLine(days, reservation, lineItems);
			AddMaidServiceLines(reservation, lineItems);
		}

		return lineItems;
	}


	#region Private Methods
	private void GetFirstMonthLines(Reservation reservation, List<LedgerLine> lines)
	{
		if (reservation.DepositType == DepositType.Deposit)
			lines.Add(new LedgerLine { TransactionType = TransactionType.Deposit, Description = "Deposit", Amount = reservation.Deposit });
		if (reservation.DepositType == DepositType.SDW)
			lines.Add(new LedgerLine { TransactionType = TransactionType.Deposit, Description = "SDW", Amount = reservation.Deposit });
		AddMaidServiceLines(reservation, lines);
	}

	private List<LedgerLine> GetLastMonthLines(Reservation reservation, List<LedgerLine> lines)
	{
		lines.Add(new LedgerLine { TransactionType = TransactionType.Charge, Description = "Departure Fee", Amount = reservation.DepartureFee });
		return lines;

	}

	private void AddRentalLine(int days, Reservation reservation, List<LedgerLine> lines)
	{
		if (reservation.BillingType == BillingType.Monthly)
			lines.Add(new LedgerLine { TransactionType = TransactionType.Charge, Description = "Rent", Amount = reservation.BillingRate });
		else
			lines.Add(new LedgerLine { TransactionType = TransactionType.Charge, Description = "Rent", Amount = days * reservation.BillingRate });
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
			lines.Add(new LedgerLine { TransactionType = TransactionType.Charge, Description = $"Maid Service ({maidServices} times)", Amount = maidServices * reservation.MaidServiceFee });
	}

	private static int CalculateNumberOfDays(DateTimeOffset startDate, DateTimeOffset endDate, BillingType billingType)
	{
		DateTime start = startDate.Date;
		DateTime end = endDate.Date;
		if (end < start) return 0;

		var days = (end - start).Days;
		if (billingType == BillingType.Nightly)
			return days;
		else // daily or monthly
			return days + 1;
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
