using Grpc.Core;

namespace Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

public class AlreadyExistsException(string message, Exception? innerException = null) : RpcException(new(StatusCode.AlreadyExists, message, innerException));
