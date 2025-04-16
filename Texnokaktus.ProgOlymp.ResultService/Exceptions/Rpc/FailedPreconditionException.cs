using Grpc.Core;

namespace Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

public class FailedPreconditionException(string message, Exception? innerException = null) : RpcException(new(StatusCode.FailedPrecondition, message, innerException));
