using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.Domain;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers.Abstractions;

public interface IFullResultQueryHandler : IQueryHandler<FullResultQuery, ContestResults?>;
