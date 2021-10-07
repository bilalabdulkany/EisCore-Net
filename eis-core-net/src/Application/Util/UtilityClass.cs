using System;
using System.Net;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EisCore.Application.Util
{
    public class UtilityClass
    {
        public static string GetLocalIpAddress(){
            if(!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()){
                return null;
            }
            IPHostEntry hostEntry= Dns.GetHostEntry(Dns.GetHostName());
            foreach(var ip in hostEntry.AddressList){
                if(ip.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork){
                    return ip.ToString();
                }
            }
            return null;
        }

        public static void ConsumeEvent(EisEvent eisEvent, string queueName, EventHandlerRegistry eventRegistry, string sourceName, ILogger log)
        {
            IMessageProcessor messageProcessor = eventRegistry.GetMessageProcessor();
            if (messageProcessor == null)
            {
                log.LogError("{app}: No message handler found for the event ID {id} in queue {queue}", sourceName, eisEvent.EventID, queueName);
                throw new Exception("No MessageProcessor found for the queue");
            }
            try
            {
                log.LogInformation("{app}: message with event {event} received", sourceName, eisEvent);
                messageProcessor.Process(eisEvent.Payload, eisEvent.EventType);
            }
            catch (Exception e)
            {
                log.LogError("{app}: Processing of message with id {id} failed with error {er}", sourceName, eisEvent.EventID, e.StackTrace);
                throw new Exception($"Processing event with ID > {eisEvent.EventID} failed > {e.Message}");
            }
        }

    }
}