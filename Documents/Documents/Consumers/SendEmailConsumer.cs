﻿using Documents.Application.Services;
using Documents.Contracts;

using MassTransit;

namespace Documents.Consumers;

public class SendEmailConsumer : IConsumer<SendEmail>
{
    private readonly IEmailService _emailService;

    public SendEmailConsumer(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Consume(ConsumeContext<SendEmail> context)
    {
        var message = context.Message;

        await _emailService.SendEmail(message.Recipient, message.Subject, message.Body);
    }
}