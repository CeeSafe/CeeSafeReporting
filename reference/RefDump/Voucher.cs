// The ERP's VoucherPrintModel (MurshiSoft/Accounts/Vouchers), as VoucherView.frx reads it: plain properties, read from
// the test JSON. (The ERP computes Amount, AmountInWords and the totals; the JSON carries them.)

public class VoucherCompanyInfo
{
    public byte[]? CompanyLogo { get; set; }
    public string? ACompanyName { get; set; }
    public string? ALine1 { get; set; }
    public string? ALine2 { get; set; }
    public string? ALine3 { get; set; }
    public string? ALine4 { get; set; }
    public string? ECompanyName { get; set; }
    public string? ELine1 { get; set; }
    public string? ELine2 { get; set; }
    public string? ELine3 { get; set; }
    public string? ELine4 { get; set; }
    public string? SponsorNo { get; set; }
    public string? SponsorName { get; set; }
    public string? SponsorAddress { get; set; }
    public string? SponsorTel { get; set; }
}

public class VoucherDetailPrint
{
    public string? CostCenterName { get; set; }
    public string? VoucherNo { get; set; }
    public string? AccountNo { get; set; }
    public string? AccountName { get; set; }
    public string? Details2 { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public int SerialNo { get; set; }
    public int CostCenterID { get; set; }
    public string? DocumentNo { get; set; }
}

public class VoucherPrint
{
    public VoucherCompanyInfo? CompanyInfo { get; set; }
    public byte[]? Logo { get; set; }
    public string? BranchName { get; set; }
    public decimal Amount { get; set; }
    public string? AmountInWords { get; set; }
    public List<VoucherDetailPrint>? PrintLineItems { get; set; }
    public bool ShowDetails { get; set; }
    public string? UserInterface { get; set; }
    public string? VoucherNo { get; set; }
    public DateTime VoucherDate { get; set; }
    public string? OtherNo { get; set; }
    public string? BillTo { get; set; }
    public string? Details { get; set; }
    public string? UserName { get; set; }
    public int VoucherTypeID { get; set; }
    public string? VoucherTypeName { get; set; }
    public string? ChequeNo { get; set; }
    public DateTime ChequeDueDate { get; set; }
    public int ChequeType { get; set; }
    public bool PendingInvoice { get; set; }
    public int FinancialYear { get; set; }
    public int sno { get; set; }
    public int BranchID { get; set; }
    public string? VoucherDate2 { get; set; }
    public DateTime AccountingDate { get; set; }
    public bool Posted { get; set; }
    public int PaymentMethodID { get; set; }
    public decimal BalanceAmount { get; set; }
    public int PaymentModeID { get; set; }
    public string? VoucherTypeMenuName { get; set; }
    public decimal DebitAmountTotal { get; set; }
    public decimal CreditAmountTotal { get; set; }
    public List<VoucherDetailPrint>? VoucherDetails { get; set; }
}
