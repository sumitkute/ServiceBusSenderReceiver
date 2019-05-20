using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBusSenderReceiver
{
    public class QueueOps
    {
        public async Task Run(string ServiceBusConnectionString, string messageQueueName)
        {
            var cts = new CancellationTokenSource();
            var sendTask = SendMessagesAsync(ServiceBusConnectionString, messageQueueName, 1);
            //var receiver = new MessageReceiver(ServiceBusConnectionString, messageQueueName, ReceiveMode.ReceiveAndDelete);
            //receiver.OperationTimeout = TimeSpan.FromHours(2);
            //var receiveTask = ReceiveMessagesAsync(ServiceBusConnectionString, messageQueueName, cts.Token);
            //var receiveTask = receiver.ReceiveAsync(50);

            await Task.WhenAll(
                // Task.WhenAny(
                //    Task.Run(() => Console.ReadKey()),
                //    //Task.Delay(TimeSpan.FromSeconds(5))
                //).ContinueWith((t) => cts.Cancel()),
                sendTask
                //receiveTask
                );

            //await receiver.CloseAsync();
        }
        async Task SendMessagesAsync(string ServiceBusConnectionString, string messageQueueName, int numberOfMessagesToSend)
        {
            IQueueClient queueCnt = new QueueClient(ServiceBusConnectionString, messageQueueName);
            queueCnt.OperationTimeout = TimeSpan.FromHours(2);
            IList<Message> messages = new List<Message>();
            try
            {
                for (var i = 0; i < numberOfMessagesToSend; i++)
                {
                    // Create a new message to send to the queue
                    string messageBody = $"Message {i}";
                    var message = new Message(Encoding.UTF8.GetBytes(messageBody));
                    messages.Add(message);
                }
                // Write the body of the message to the console
                Console.WriteLine($"Sending messages: {messages.Count}");

                // Send the message to the queue
                await queueCnt.SendAsync(messages);

                await queueCnt.CloseAsync();

                Console.WriteLine($"Messages Send: {messages.Count}");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }
        async Task<dynamic> ReceiveMessagesAsync(string connectionString, string queueName, CancellationToken cancellationToken)
        {
            var receiver = new MessageReceiver(connectionString, queueName, ReceiveMode.PeekLock);
            dynamic queuemetdata = "";

            var doneReceiving = new TaskCompletionSource<bool>();
            // close the receiver and factory when the CancellationToken fires 
            cancellationToken.Register(
                async () =>
                {
                    await receiver.CloseAsync();
                    doneReceiving.SetResult(true);
                });

            // register the RegisterMessageHandler callback
            receiver.RegisterMessageHandler(
                async (message, cancellationToken1) =>
                {
                    if (message.Label != null &&
                        message.ContentType != null &&
                        //message.Label.Equals("Scientist", StringComparison.InvariantCultureIgnoreCase) &&
                        message.ContentType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var body = message.Body;

                        queuemetdata = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(body));

                        lock (Console.Out)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(
                                "\t\t\t\tMessage received: \n\t\t\t\t\t\tMessageId = {0}, \n\t\t\t\t\t\tSequenceNumber = {1}, \n\t\t\t\t\t\tEnqueuedTimeUtc = {2}," +
                                "\n\t\t\t\t\t\tExpiresAtUtc = {5}, \n\t\t\t\t\t\tContentType = \"{3}\", \n\t\t\t\t\t\tSize = {4},  \n\t\t\t\t\t\tContent: [ firstName = {6}, name = {7} ]",
                                message.MessageId,
                                message.SystemProperties.SequenceNumber,
                                message.SystemProperties.EnqueuedTimeUtc,
                                message.ContentType,
                                message.Size,
                                message.ExpiresAtUtc,
                                queuemetdata.name);
                            Console.ResetColor();
                        }
                        await receiver.CompleteAsync(message.SystemProperties.LockToken);
                    }
                    else
                    {
                        await receiver.DeadLetterAsync(message.SystemProperties.LockToken); //, "ProcessingError", "Don't know what to do with this message");
                    }
                },
                new MessageHandlerOptions((e) => LogMessageHandlerException(e)) { AutoComplete = false, MaxConcurrentCalls = 1 });

            await doneReceiving.Task;
            return queuemetdata;
        }
        Task LogMessageHandlerException(ExceptionReceivedEventArgs e)
        {
            Console.WriteLine("Exception: \"{0}\" {0}", e.Exception.Message, e.ExceptionReceivedContext.EntityPath);
            return Task.CompletedTask;
        }

    }
}
