﻿using System.Text.Json.Serialization;

using Azure.Identity;
using Azure.Storage.Blobs;

using Documents;
using Documents.Application;
using Documents.Application.Commands;
using Documents.Application.Common.Interfaces;
using Documents.Application.Common.Models;
using Documents.Application.Queries;
using Documents.Application.Services;
using Documents.Consumers;
using Documents.Contracts;
using Documents.Infrastructure;
using Documents.Infrastructure.Persistence;
using Documents.Services;

using MassTransit;
using MassTransit.MessageData;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var Configuration = builder.Configuration;

builder.Services
    .AddApplication()
    .AddInfrastructure(Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddControllers();

// Set the JSON serializer options
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    // options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddMediatR(typeof(Program));

builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerDocument(c =>
{
    c.Title = "Documents API";
    c.Version = "0.1";
});

// Add the reverse proxy capability to the server
var proxyBuilder = builder.Services.AddReverseProxy();
// Initialize the reverse proxy from the "ReverseProxy" section of configuration
proxyBuilder.LoadFromConfig(Configuration.GetSection("ReverseProxy"));

// TODO: Switch out for Azure Storage later
IMessageDataRepository messageDataRepository = new InMemoryMessageDataRepository();

builder.Services.AddSingleton(messageDataRepository);

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    //x.AddConsumers(typeof(Program).Assembly);

    x.AddConsumer<CreateDocumentFromTemplateConsumer>();

    x.AddRequestClient<CreateDocumentFromTemplate>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseMessageData(messageDataRepository);

        cfg.ConfigureEndpoints(context);
    });
})
.AddMassTransitHostedService(true)
.AddGenericRequestClient();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRazorTemplateCompiler, RazorTemplateCompiler>();
builder.Services.AddScoped<IPdfGenerator, PdfGenerator>();
builder.Services.AddScoped<IFileUploaderService, FileUploaderService>();

//IronPdf.Logging.Logger.EnableDebugging = true;
//IronPdf.Logging.Logger.LogFilePath = "Default.log"; //May be set to a directory name or full file
//IronPdf.Logging.Logger.LoggingMode = IronPdf.Logging.Logger.LoggingModes.All;

builder.Services.AddAzureClients(builder =>
{
    // Add a KeyVault client
    //builder.AddSecretClient(keyVaultUrl);

    // Add a Storage account client
    builder.AddBlobServiceClient(Configuration.GetConnectionString("Azure:Storage"))
                    .WithVersion(BlobClientOptions.ServiceVersion.V2019_07_07);

    // Use DefaultAzureCredential by default
    builder.UseCredential(new DefaultAzureCredential());
});

CultureInfo? culture = new("sv-SE");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi3();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRouting();

app.MapReverseProxy();

app.MapGet("/Documents", async (int page, int pageSize, IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(new GetDocuments(page, pageSize), cancellationToken))
    .WithName("Documents_GetDocuments")
    .WithTags("Documents")
    .Produces<ItemsResult<DocumentDto>>(StatusCodes.Status200OK);

app.MapGet("/Documents/{id}", async (string id, IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(new GetDocument(id), cancellationToken))
    .WithName("Documents_GetDocument")
    .WithTags("Documents")
    .Produces<DocumentDto>(StatusCodes.Status200OK);

app.MapPost("/Documents/{id}/Rename", async (string id, string newName, IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(new RenameDocument(id, newName), cancellationToken))
    .WithName("Documents_RenameDocument")
    .WithTags("Documents")
    .Produces(StatusCodes.Status200OK);

/*
app.MapPost("/GenerateDocument", async (string templateId, [FromBody] string model, IMediator mediator) =>
{
    DocumentFormat documentFormat = DocumentFormat.Html;

    var stream = await mediator.Send(new GenerateDocument(templateId, model));
    return Results.File(stream, documentFormat == DocumentFormat.Html ? "application/html" : "application/pdf");
})
.WithName("Documents_GenerateDocument")
.WithTags("Documents")
//.RequireAuthorization()
.Produces(StatusCodes.Status200OK);
*/

/*
app.MapPost("/UploadDocument", async ([FromBody] UploadDocument model, IMediator mediator) =>
{
    await mediator.Send(model);
})
.WithName("Documents_UpladDocument")
.WithTags("Documents")
//.RequireAuthorization()
.Produces(StatusCodes.Status200OK);
*/

app.MapControllers();

//await SeedData.EnsureSeedData(app);

app.Run();