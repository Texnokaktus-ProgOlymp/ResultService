using Texnokaktus.ProgOlymp.Cqrs;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Commands.Handlers;

public interface ICreateResultAdjustmentCommandHandler : ICommandHandler<CreateResultAdjustmentCommand, int>;
