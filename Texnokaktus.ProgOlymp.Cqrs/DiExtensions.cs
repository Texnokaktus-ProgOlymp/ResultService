using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Texnokaktus.ProgOlymp.Cqrs;

public static class DiExtensions
{
    public static IServiceCollection AddQueryHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler, TQuery, TResult>(this IServiceCollection services, ServiceLifetime lifetime) where THandler : class, IQueryHandler<TQuery, TResult>
    {
        services.Add(ServiceDescriptor.Describe(typeof(IQueryHandler<TQuery, TResult>), typeof(THandler), lifetime));
        return services;
    }

    public static IServiceCollection AddQueryHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler, TQuery, TResult>(this IServiceCollection services, Func<IServiceProvider, THandler> implementationFactory, ServiceLifetime lifetime) where THandler : class, IQueryHandler<TQuery, TResult>
    {
        services.Add(ServiceDescriptor.Describe(typeof(IQueryHandler<TQuery, TResult>), implementationFactory, lifetime));
        return services;
    }

    public static IServiceCollection AddCommandHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler, TQuery>(this IServiceCollection services, ServiceLifetime lifetime) where THandler : class, ICommandHandler<TQuery>
    {
        services.Add(ServiceDescriptor.Describe(typeof(ICommandHandler<TQuery>), typeof(THandler), lifetime));
        return services;
    }

    public static IServiceCollection AddCommandHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler, TQuery>(this IServiceCollection services, Func<IServiceProvider, THandler> implementationFactory, ServiceLifetime lifetime) where THandler : class, ICommandHandler<TQuery>
    {
        services.Add(ServiceDescriptor.Describe(typeof(ICommandHandler<TQuery>), implementationFactory, lifetime));
        return services;
    }

    public static IServiceCollection AddCommandHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler, TQuery, TResult>(this IServiceCollection services, ServiceLifetime lifetime) where THandler : class, ICommandHandler<TQuery, TResult>
    {
        services.Add(ServiceDescriptor.Describe(typeof(ICommandHandler<TQuery, TResult>), typeof(THandler), lifetime));
        return services;
    }

    public static IServiceCollection AddCommandHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler, TQuery, TResult>(this IServiceCollection services, Func<IServiceProvider, THandler> implementationFactory, ServiceLifetime lifetime) where THandler : class, ICommandHandler<TQuery, TResult>
    {
        services.Add(ServiceDescriptor.Describe(typeof(ICommandHandler<TQuery, TResult>), implementationFactory, lifetime));
        return services;
    }
}
