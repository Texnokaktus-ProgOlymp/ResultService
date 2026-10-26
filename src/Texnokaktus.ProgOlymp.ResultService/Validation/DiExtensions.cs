using FluentValidation;
using Texnokaktus.ProgOlymp.ResultService.Models.Requests;
using GrpcRequests = Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using GrpcValidators = Texnokaktus.ProgOlymp.ResultService.Validation.Grpc;

namespace Texnokaktus.ProgOlymp.ResultService.Validation;

public static class DiExtensions
{
    public static IServiceCollection AddRequestValidation(this IServiceCollection services) =>
        services.AddScoped<IValidator<GetResultsRequest>, GetResultsRequestValidator>()
                .AddScoped<IValidator<GrpcRequests.GetContestRequest>, GrpcValidators.GetContestRequestValidator>()
                .AddScoped<IValidator<GrpcRequests.AddContestRequest>, GrpcValidators.AddContestRequestValidator>()
                .AddScoped<IValidator<GrpcRequests.AddProblemRequest>, GrpcValidators.AddProblemRequestValidator>()
                .AddScoped<IValidator<GrpcRequests.AddResultRequest>, GrpcValidators.AddResultRequestValidator>()
                .AddScoped<IValidator<GrpcRequests.AddResultAdjustmentRequest>, GrpcValidators.AddResultAdjustmentRequestValidator>()
                .AddScoped<IValidator<GrpcRequests.GetResultsRequest>, GrpcValidators.GetResultsRequestValidator>()
                .AddScoped<IValidator<GrpcRequests.GetResultsByParticipantRequest>, GrpcValidators.GetResultsByParticipantRequestValidator>();
}
