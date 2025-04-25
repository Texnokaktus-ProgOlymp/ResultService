namespace Texnokaktus.ProgOlymp.ResultService.Logic.Services.Abstractions;

public interface IParticipantService
{
    Task<int?> GetParticipantIdAsync(int contestId, int userId);
}
