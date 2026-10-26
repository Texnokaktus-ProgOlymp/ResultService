using FluentValidation;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;

namespace Texnokaktus.ProgOlymp.ResultService.Validation.Grpc;

public class GetResultsRequestValidator : AbstractValidator<GetResultsRequest>
{
    public GetResultsRequestValidator()
    {
        RuleFor(request => request.ContestName).NotEmpty();
        RuleFor(request => request.Stage).IsInEnum();
    }
}
