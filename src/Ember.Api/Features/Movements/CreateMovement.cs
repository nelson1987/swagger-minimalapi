﻿using Ember.Api.Features.Users;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
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
    public record Success(string Message);
    public record Fail(string Message);
    public class CommandExample : IMultipleExamplesProvider<Command>
    {
        public IEnumerable<SwaggerExample<Command>> GetExamples()
        {
            yield return SwaggerExample.Create("Manager", new Command(0.50M));
            yield return SwaggerExample.Create("Employee", new Command(50.49M));
        }
    }
    public class TagDescriptionsDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Tags = new List<OpenApiTag> {
            new OpenApiTag { Name = "Products", Description = "Browse/manage the product catalog" },
            new OpenApiTag { Name = "Orders", Description = "Submit orders" }
            };
            swaggerDoc.Components.Schemas
                .Add("Success", new OpenApiSchema()
            {
                Description = "Description",
                ReadOnly = false,
                Type = "string",
                Title = "Title"
            });
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
    public static RouteHandlerBuilder AddMetaData(this RouteHandlerBuilder endpoint, string tag, string summary = null, string description = null)
    {
        endpoint.WithTags(tag);
        endpoint.WithMetadata(new SwaggerOperationAttribute(summary, description))
                .WithMetadata(new SwaggerResponseAttribute(200, "Sucess", type: typeof(Success)))
                .WithMetadata(new SwaggerResponseAttribute(500, "Some failure", type: typeof(Fail)))
                .WithMetadata(new SwaggerRequestExampleAttribute(typeof(Command), typeof(CommandExample)))
                .WithMetadata(new SwaggerSchemaFilterAttribute(typeof(Command)))
                .WithMetadata(new SwaggerParameterAttribute("Parameter"))
                .WithMetadata(new SwaggerRequestBodyAttribute("Body"))
                .WithMetadata(new SwaggerSchemaFilterAttribute(typeof(TagDescriptionsDocumentFilter)));
        //endpoint.WithMetadata(new SwaggerResponseAttribute(500, type: typeof(ErrorResponseModel)))
        //.WithMetadata(new SwaggerResponseAttribute(400, type: typeof(ErrorResponseModel)))
        //.WithMetadata(new SwaggerResponseAttribute(404, type: typeof(ErrorResponseModel)))
        //.WithMetadata(new SwaggerResponseAttribute(422, type: typeof(ErrorResponseModel)))
        //.WithMetadata(new SwaggerResponseAttribute(304, type: typeof(ErrorResponseModel)));

        return endpoint;
    }
    public static IEndpointRouteBuilder MapCreateMovement(this IEndpointRouteBuilder app)
    {
        app
            .MapPost("app/movements", async (Command command, IHandler handler, CancellationToken cancellationToken) =>
            {
                var userId = await handler.Handle(command, cancellationToken);
                return Results.Created("app/users", userId);
            })
            .AddEndpointFilter<ValidationFilter<Command>>()
            .AddMetaData("movements", "returns clients", "more description on get `Movements`");
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