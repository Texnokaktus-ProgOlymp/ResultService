using FluentValidation;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;

namespace Texnokaktus.ProgOlymp.ResultService.Validation.Grpc;

public class GetResultsByParticipantRequestValidator : AbstractValidator<GetResultsByParticipantRequest>
{
    public GetResultsByParticipantRequestValidator()
    {
        RuleFor(request => request.ContestName).NotEmpty();
        RuleFor(request => request.Stage).IsInEnum();
        RuleFor(request => request.ParticipantId).GreaterThan(0);
    }
}
