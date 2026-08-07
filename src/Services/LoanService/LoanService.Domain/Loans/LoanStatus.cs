namespace LoanService.Domain.Loans;

public enum LoanStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    Approved = 4,
    Rejected = 5,
    Funded = 6,
    Closed = 7,
    Cancelled = 8
}