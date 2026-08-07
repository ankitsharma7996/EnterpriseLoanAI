using LoanService.Application.Abstractions.Messaging;
using LoanService.Application.Abstractions.Persistence;
using LoanService.Application.Loans.Events;
using LoanService.Domain.Loans;
using MediatR;

namespace LoanService.Application.Loans.CreateLoan;

internal sealed class CreateLoanCommandHandler
    : IRequestHandler<CreateLoanCommand, CreateLoanResult>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IOutboxWriter _outboxWriter;

    public CreateLoanCommandHandler(
        ILoanRepository loanRepository,
        IOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _outboxWriter = outboxWriter;
    }

    public async Task<CreateLoanResult> Handle(
     CreateLoanCommand request,
     CancellationToken cancellationToken)
    {
        var normalizedLoanNumber =
            request.LoanNumber.Trim().ToUpperInvariant();

        var loanNumberExists =
            await _loanRepository.ExistsByLoanNumberAsync(
                normalizedLoanNumber,
                cancellationToken);

        if (loanNumberExists)
        {
            throw new InvalidOperationException(
                $"Loan number '{normalizedLoanNumber}' already exists.");
        }

        var occurredOnUtc = _timeProvider.GetUtcNow();

        var loan = Loan.Create(
            Guid.NewGuid(),
            normalizedLoanNumber,
            request.CustomerId,
            request.RequestedAmount,
            request.Currency,
            occurredOnUtc);

        var integrationEvent =
            new LoanCreatedIntegrationEvent
            {
                EventId = Guid.NewGuid(),
                LoanId = loan.Id,
                CustomerId = loan.CustomerId,
                LoanNumber = loan.LoanNumber,
                RequestedAmount = loan.RequestedAmount,
                Currency = loan.Currency,
                Status = loan.Status.ToString(),
                CorrelationId = request.CorrelationId,
                AggregateVersion = loan.Version,
                OccurredOnUtc = occurredOnUtc
            };

        await _loanRepository.AddAsync(
            loan,
            cancellationToken);

        await _outboxWriter.AddAsync(
            integrationEvent,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new CreateLoanResult(
            loan.Id,
            loan.LoanNumber,
            loan.Status.ToString());
    }
}