using FluentValidation;
using Texnokaktus.ProgOlymp.ResultService.Models.Requests;

namespace Texnokaktus.ProgOlymp.ResultService.Validation;

public class GetResultsRequestValidator : AbstractValidator<GetResultsRequest>
{
    public GetResultsRequestValidator()
    {
        RuleFor(request => request.ContestName).NotEmpty().OverridePropertyName("contestName");
        RuleFor(request => request.Stage).IsInEnum().OverridePropertyName("stage");
    }
}
