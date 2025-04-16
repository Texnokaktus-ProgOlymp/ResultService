using Grpc.Core;

namespace Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

public class NotFoundException(string message, Exception? innerException = null) : RpcException(new(StatusCode.NotFound, message, innerException));
