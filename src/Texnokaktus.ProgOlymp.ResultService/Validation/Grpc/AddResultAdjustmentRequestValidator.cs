using FluentValidation;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;

namespace Texnokaktus.ProgOlymp.ResultService.Validation.Grpc;

public class AddResultAdjustmentRequestValidator : AbstractValidator<AddResultAdjustmentRequest>
{
    public AddResultAdjustmentRequestValidator()
    {
        RuleFor(request => request.ContestName).NotEmpty();
        RuleFor(request => request.Stage).IsInEnum();
        RuleFor(request => request.Alias).NotEmpty();
        RuleFor(request => request.ParticipantId).GreaterThan(0);
        RuleFor(request => request.Adjustment).NotNull();
    }
}
