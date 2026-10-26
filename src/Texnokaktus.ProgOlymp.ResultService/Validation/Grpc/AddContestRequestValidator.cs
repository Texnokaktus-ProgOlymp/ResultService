using FluentValidation;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;

namespace Texnokaktus.ProgOlymp.ResultService.Validation.Grpc;

public class AddContestRequestValidator : AbstractValidator<AddContestRequest>
{
    public AddContestRequestValidator()
    {
        RuleFor(request => request.ContestName).NotEmpty();
        RuleFor(request => request.Stage).IsInEnum();
        RuleFor(request => request.StageId).GreaterThan(0);
    }
}
