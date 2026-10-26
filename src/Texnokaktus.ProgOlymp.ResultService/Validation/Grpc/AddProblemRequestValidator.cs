using FluentValidation;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;

namespace Texnokaktus.ProgOlymp.ResultService.Validation.Grpc;

public class AddProblemRequestValidator : AbstractValidator<AddProblemRequest>
{
    public AddProblemRequestValidator()
    {
        RuleFor(request => request.ContestName).NotEmpty();
        RuleFor(request => request.Stage).IsInEnum();
        RuleFor(request => request.Alias).NotEmpty();
        RuleFor(request => request.Name).NotEmpty();
    }
}
