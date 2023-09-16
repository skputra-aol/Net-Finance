﻿using Documents.Client;

using Invoices.Application;
using Invoices.Application.Common.Interfaces;
using Invoices.Application.Queries;
using Invoices.Application.Commands;
using Invoices.Domain.Enums;
using Invoices.Infrastructure;
using Invoices.Services;

using MassTransit;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Json;
using Invoices.Infrastructure.Persistence;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var Configuration = builder.Configuration;

builder.Services
    .AddApplication()
    .AddInfrastructure(Configuration);

builder.Services.AddControllers();

// Set the JSON serializer options
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    // options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerDocument(c =>
{
    c.Title = "Invoices API";
    c.Version = "0.1";
});

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    //x.AddConsumers(typeof(Program).Assembly);

    //x.AddRequestClient<TransactionBatch>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
})
.AddMassTransitHostedService(true)
.AddGenericRequestClient();

builder.Services.AddDocumentsClients((sp, http) =>
{
    http.BaseAddress = new Uri($"{Configuration.GetServiceUri("nginx")}/documents/");
});

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

app.MapGet("/", () => "Hello World!");

/*
app.MapGet("/invoices", async (int page, int pageSize, [FromQuery] InvoiceStatus[]? status, IMediator mediator)
    => await mediator.Send(new GetInvoices(page, pageSize, status)))
    .WithName("Invoices_GetInvoices")
    .WithTags("Invoices")
    .Produces<ItemsResult<InvoiceDto>>(StatusCodes.Status200OK);
*/

app.MapGet("/Invoices/{invoiceId}", async (int invoiceId, IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(new GetInvoice(invoiceId), cancellationToken))
    .WithName("Invoices_GetInvoice")
    .WithTags("Invoices")
    .Produces<InvoiceDto>(StatusCodes.Status200OK);

app.MapGet("/Invoices/{invoiceId}/File", async (int invoiceId, IMediator mediator, CancellationToken cancellationToken)
    => Results.File(await mediator.Send(new GenerateInvoiceFile(invoiceId), cancellationToken), "application/html", $"{invoiceId}.html"))
    .WithName("Invoices_GetInvoiceFile")
    .WithTags("Invoices")
    .Produces<FileResult>(StatusCodes.Status200OK);

app.MapPost("/Invoices/{invoiceId}/Items", async (int invoiceId, 
    ProductType productType, string description, decimal unitPrice, string unit, double vatRate, double quantity, 
    IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(new AddItem(invoiceId, productType, description, unitPrice, unit, vatRate, quantity), cancellationToken))
    .WithName("Invoices_AddItem")
    .WithTags("Invoices")
    .Produces<InvoiceItemDto>(StatusCodes.Status200OK);

app.MapPost("/Invoices", async (CreateInvoice command, IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(command, cancellationToken))
    .WithName("Invoices_CreateInvoice")
    .WithTags("Invoices")
    .Produces<InvoiceDto>(StatusCodes.Status200OK);

app.MapPut("/Invoices/{invoiceId}/Status", async (int invoiceId, InvoiceStatus status, 
IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(new SetInvoiceStatus(invoiceId, status), cancellationToken))
    .WithName("Invoices_SetInvoiceStatus")
    .WithTags("Invoices")
    .Produces(StatusCodes.Status200OK);

app.MapPut("/Invoices/{invoiceId}/PaidAmount", async (int invoiceId, decimal amount, 
IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(new SetPaidAmount(invoiceId, amount), cancellationToken))
    .WithName("Invoices_SetPaidAmount")
    .WithTags("Invoices")
    .Produces(StatusCodes.Status200OK);

app.MapPut("/Invoices/{invoiceId}/Date", async (int invoiceId, DateTime date, 
IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(new SetDate(invoiceId, date), cancellationToken))
    .WithName("Invoices_SetDate")
    .WithTags("Invoices")
    .Produces(StatusCodes.Status200OK);

app.MapPut("/Invoices/{invoiceId}/Type", async (int invoiceId, InvoiceType type, 
IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(new SetType(invoiceId, type), cancellationToken))
    .WithName("Invoices_SetType")
    .WithTags("Invoices")
    .Produces(StatusCodes.Status200OK);

app.MapPut("/Invoices/{invoiceId}/DueDate", async (int invoiceId, DateTime dueDate, 
IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(new SetDueDate(invoiceId, dueDate), cancellationToken))
    .WithName("Invoices_SetDueDate")
    .WithTags("Invoices")
    .Produces(StatusCodes.Status200OK);

app.MapPut("/Invoices/{invoiceId}/Reference", async (int invoiceId, string? reference, 
IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(new SetReference(invoiceId, reference), cancellationToken))
    .WithName("Invoices_SetReference")
    .WithTags("Invoices")
    .Produces(StatusCodes.Status200OK);

app.MapPut("/Invoices/{invoiceId}/Note", async (int invoiceId, string? note, 
IMediator mediator, CancellationToken cancellationToken)
    => await mediator.Send(new SetNote(invoiceId, note), cancellationToken))
    .WithName("Invoices_SetNote")
    .WithTags("Invoices")
    .Produces(StatusCodes.Status200OK);

app.MapControllers();

//await SeedData.EnsureSeedData(app);

app.Run();