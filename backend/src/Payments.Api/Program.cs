using Payments.Api.Shared.Extensions;
using Payments.Application;
using Payments.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath)
    .AddApi(builder.Configuration);

var app = builder.Build();
app.UseApi();
app.Run();
