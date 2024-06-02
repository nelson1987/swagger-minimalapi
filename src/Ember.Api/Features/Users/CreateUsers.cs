using FluentValidation;

namespace Ember.Api.Features.Users;
public static class CreateUsers
{
    public record CreateUserCommand(string UserName);
    public class User { public int Id { get; set; } }
    public interface IUserRepository
    {
        Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    }
    public class UserRepository : IUserRepository
    {
        public Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
    public interface IHandler { Task<int> Handle(CreateUserCommand command, CancellationToken cancellationToken = default); }
    public sealed class Handler : IHandler
    {
        private readonly IUserRepository _repository;

        public Handler(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> Handle(CreateUserCommand command, CancellationToken cancellationToken = default)
        {
            var user = new User();
            var response = await _repository.CreateAsync(user, cancellationToken);
            return response.Id;
        }
    }
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.UserName).NotEmpty();
        }
    }

    public static IServiceCollection AddCreateUser(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHandler, Handler>();
        services.AddScoped<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
        return services;
    }
    public static IEndpointRouteBuilder MapCreateUser(this IEndpointRouteBuilder app)
    {
        app
            .MapPost("app/users", async (CreateUserCommand command, IHandler handler, CancellationToken cancellationToken) =>
            {
                var userId = await handler.Handle(command, cancellationToken);
                return Results.Created("app/users", userId);
            })
            .AddEndpointFilter<ValidationFilter<CreateUserCommand>>();
        return app;
    }
}
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var obj = context.Arguments.FirstOrDefault(x => x?.GetType() == typeof(T)) as T;

        if (obj is null)
        {
            return Results.BadRequest();
        }

        var validationResult = await _validator.ValidateAsync(obj);

        if (!validationResult.IsValid)
        {
            //return Results.BadRequest(string.Join("/n", validationResult.Errors));
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        return await next(context);
    }
}