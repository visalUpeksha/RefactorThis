using System;
using System.Linq;
using RefactorThis.Persistence;

namespace RefactorThis.Domain
{
	public class InvoiceService
	{
		private readonly InvoiceRepository _invoiceRepository;

		public InvoiceService( InvoiceRepository invoiceRepository )
		{
			_invoiceRepository = invoiceRepository;
		}

		public string ProcessPayment( Payment payment )
		{
			var inv = _invoiceRepository.GetInvoice( payment.Reference );
			var responseMessage = string.Empty;
			decimal taxPortion = 0.14m;

            if ( inv == null )
			{
				throw new InvalidOperationException( "There is no invoice matching this payment" );
			}
			else
			{
                ProcessInvoice(ref inv, ref responseMessage, payment, taxPortion);
			}
			inv.Save();
			return responseMessage;
		}

        private void ProcessInvoice(ref Invoice inv, ref string responseMessage, Payment payment, decimal taxPortion)
        {
            if (inv.Amount == 0)
            {
                ValidateInvoice(ref inv, ref responseMessage);
            }
            else
            {
                processInvoicePayments(ref inv, ref responseMessage, payment, taxPortion);
            }
        }

        private void processInvoicePayments(ref Invoice inv, ref string responseMessage, Payment payment, decimal taxPortion)
        {
            if (inv.Payments != null && inv.Payments.Any())
            {
                ProcessPaymentsForInvoicesWithPayments(ref inv, ref responseMessage, payment, taxPortion);
            }
            else
            {
                ProcessPaymentForNullInvoice(ref responseMessage, ref inv, payment, taxPortion);
            }
        }

        private void ValidateInvoice(ref Invoice inv, ref string responseMessage)
        {
            if (inv.Payments == null || !inv.Payments.Any())
            {
                responseMessage = "no payment needed";
            }
            else
            {
                throw new InvalidOperationException("The invoice is in an invalid state, it has an amount of 0 and it has payments.");
            }
        }

        private void ProcessPaymentsForInvoicesWithPayments(ref Invoice inv, ref string responseMessage, Payment payment, decimal taxPortion)
        {
            if (inv.Payments.Sum(x => x.Amount) != 0 && inv.Amount == inv.Payments.Sum(x => x.Amount))
            {
                responseMessage = "invoice was already fully paid";
            }
            else if (inv.Payments.Sum(x => x.Amount) != 0 && payment.Amount > (inv.Amount - inv.AmountPaid))
            {
                responseMessage = "the payment is greater than the partial amount remaining";
            }
            else
            {
                ProcessPaidInvoices(ref inv, ref responseMessage, payment, taxPortion);
            }
        }

        private void ProcessPaidInvoices(ref Invoice inv, ref string responseMessage, Payment payment, decimal taxPortion)
        {
            if ((inv.Amount - inv.AmountPaid) == payment.Amount)
            {
                ProcessPaymentForFullyPaidInvoice(ref inv, payment, ref responseMessage, "final partial payment received, invoice is now fully paid", taxPortion);
            }
            else
            {
                ProcessPaymentForInvoiceWithBalance(ref inv, ref responseMessage, "another partial payment received, still not fully paid", payment, taxPortion);
            }
        }

        private void ProcessPaymentForFullyPaidInvoice(ref Invoice inv, Payment payment, ref string responseMessage, string responseMessageContent, decimal taxPortion)
        {
            switch (inv.Type)
            {
                case InvoiceType.Standard:
                    SetInvoiceData(ref inv, payment, ref responseMessage, "final partial payment received, invoice is now fully paid");
                    break;
                case InvoiceType.Commercial:
                    SetInvoiceData(ref inv, payment, ref responseMessage, "another partial payment received, still not fully paid", taxPortion);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ProcessPaymentForInvoiceWithBalance(ref Invoice inv, ref string responseMessage,string responseMessageContent, Payment payment, decimal taxPortion)
        {
            switch (inv.Type)
            {
                case InvoiceType.Standard:
                    SetInvoiceData(ref inv, payment, ref responseMessage, responseMessageContent);
                    break;
                case InvoiceType.Commercial:
                    SetInvoiceData(ref inv, payment, ref responseMessage, responseMessageContent, taxPortion);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SetInvoiceData(ref Invoice inv, Payment payment, ref string responseMessage, string responseMessageContent, decimal? taxPortion =null)
        {
            inv.AmountPaid += payment.Amount;
			if (taxPortion != null) inv.TaxAmount += (decimal)(payment.Amount * taxPortion);
            inv.Payments.Add(payment);
            responseMessage = responseMessageContent;
        }

        private void ProcessPaymentForNullInvoice(ref string responseMessage, ref Invoice inv, Payment payment, decimal taxPortion)
        {
            if (payment.Amount > inv.Amount)
            {
                responseMessage = "the payment is greater than the invoice amount";
            }
            else if (inv.Amount == payment.Amount)
            {
                SetInvoiceDataBulkForNullInvoice(ref inv, payment, ref responseMessage, "invoice is now fully paid", taxPortion);
            }
            else
            {
                SetInvoiceDataBulkForNullInvoice(ref inv, payment, ref responseMessage, "invoice is now partially paid", taxPortion);
            }
        }

        private void SetInvoiceDataBulkForNullInvoice(ref Invoice inv, Payment payment, ref string responseMessage, string responseMessageContent, decimal taxPortion)
        {
            if (inv.Type == InvoiceType.Standard || inv.Type == InvoiceType.Commercial)
			{
                SetInvoiceDataForNullInvoice(ref inv, payment, ref responseMessage, responseMessageContent, taxPortion);
            }
			else
			{
                throw new ArgumentOutOfRangeException();
            }
        }

        private void SetInvoiceDataForNullInvoice(ref Invoice inv, Payment payment, ref string responseMessage, string responseMessageContent, Decimal? taxPortion = null )
        {
            inv.AmountPaid = payment.Amount;
			if (taxPortion.HasValue)
			{
                inv.TaxAmount = (decimal)(payment.Amount * taxPortion);
            }
            inv.Payments.Add(payment);
			responseMessage = responseMessageContent;
        }
    }
}