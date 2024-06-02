using Ember.Api.Features.Users;
using FluentValidation;
using Mapster;
using MapsterMapper;
using System.Reflection;

namespace Ember.Api.Features.Movements;
public class Movement
{
    public int Id { get; set; }
    public string Account { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
public interface IMovementRepository
{
    Task<Movement> AddAsync(Movement movement, CancellationToken cancellationToken = default);
}
public class MovementRepository : IMovementRepository
{
    public async Task<Movement> AddAsync(Movement movement, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
public static class CreateMovement
{
    public record Command(decimal Amount);
    public record Response(string Message);
    public interface IHandler { Task<int> Handle(Command command, CancellationToken cancellationToken = default); }
    public class Handler : IHandler
    {
        private readonly IMovementRepository _movementRepository;

        public Handler(IMovementRepository movementRepository)
        {
            _movementRepository = movementRepository;
        }

        public async Task<int> Handle(Command command, CancellationToken cancellationToken = default)
        {
            Movement movement = command.MapTo<Movement>();
            var response = await _movementRepository.AddAsync(movement, cancellationToken);
            return response.Id;
        }
    }
    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Amount).NotEmpty()
                .GreaterThan(0.01M);
        }
    }

    public static IServiceCollection AddCreateMovement(this IServiceCollection services)
    {
        services.AddScoped<IMovementRepository, MovementRepository>();
        services.AddScoped<IHandler, Handler>();
        services.AddScoped<IValidator<Command>, CommandValidator>();
        TypeAdapterConfig<Command, Movement>
            .NewConfig()
            .Map(dest => dest.Account, src => "Conta Bancaria")
            .Map(dest => dest.Amount, src => src.Amount)
            .Map(dest => dest.CreatedAt, src => DateTime.Now);

        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        return services;
    }
    public static IEndpointRouteBuilder MapCreateMovement(this IEndpointRouteBuilder app)
    {
        app
            .MapPost("app/movements", async (Command command, IHandler handler, CancellationToken cancellationToken) =>
            {
                var userId = await handler.Handle(command, cancellationToken);
                return Results.Created("app/users", userId);
            })
            .AddEndpointFilter<ValidationFilter<Command>>();
        return app;
    }
}
public static class MapsterExtensions
{
    public static T MapTo<T>(this object obj)
    {
        return obj.Adapt<T>();
    }
}