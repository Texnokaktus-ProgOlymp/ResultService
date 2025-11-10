using Texnokaktus.ProgOlymp.Cqrs;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Commands.Handlers.Abstractions;

public interface ICreateResultAdjustmentCommandHandler : ICommandHandler<CreateResultAdjustmentCommand, int>;
