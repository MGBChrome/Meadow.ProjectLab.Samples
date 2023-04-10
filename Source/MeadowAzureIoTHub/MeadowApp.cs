﻿using Amqp;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.Leds;
using Meadow.Hardware;
using MeadowAzureIoTHub.Views;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MeadowAzureIoTHub
{
    // Change F7FeatherV2 to F7FeatherV1 for V1.x boards
    public class MeadowApp : App<F7FeatherV2>
    {
        RgbPwmLed onboardLed;

        private static readonly Random rand = new Random();

        /// <summary>
        /// You'll need to create an IoT Hub - https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal
        /// Create a device within your IoT Hub
        /// And then generate a SAS token - this can be done via the Azure CLI 
        /// 
        /// Example
        /// az iot hub generate-sas-token
        /// --hub-name HUB_NAME 
        /// --device-id DEVICE_ID 
        /// --resource-group RESOURCE_GROUP 
        /// --login [Open Shared access policies -> Select iothubowner -> copy Primary connection string
        /// </summary>

        const string HubName = Secrets.HUB_NAME;
        const string SasToken = Secrets.SAS_TOKEN;
        const string DeviceId = Secrets.DEVICE_ID;

        // Lat/Lon Points
        static double latitude = 49.246292;
        static double longitude = -123.116226;
        const double radius = 6378;

        static readonly bool traceOn = false;

        public override async Task Initialize()
        {
            Console.WriteLine("Initializing...");
            onboardLed = new RgbPwmLed(
                Device.Pins.OnboardLedRed,
                Device.Pins.OnboardLedGreen,
                Device.Pins.OnboardLedBlue);
            onboardLed.SetColor(Color.Red);

            try
            {
                var wifi = Device.NetworkAdapters.Primary<IWiFiNetworkAdapter>();
                wifi.NetworkConnected += NetworkConnected;

                var projectLab = ProjectLab.Create();

                DisplayController.Instance.Initialize(projectLab.Display);
                DisplayController.Instance.ShowSplashScreen();
                DisplayController.Instance.StartConnectingAnimation();
            }
            catch (Exception ex)
            {
                Resolver.Log.Error($"Failed to Connect: {ex.Message}");
            }
        }

        private void NetworkConnected(INetworkAdapter sender, NetworkConnectionEventArgs args)
        {
            Console.WriteLine("Connected...");
            onboardLed.SetColor(Color.Green);
            DisplayController.Instance.StopConnectingAnimation();

            latitude = 49.246292;
            longitude = -123.116226;

            //IoTHubDataTest();
        }

        void IoTHubDataTest()
        {
            Console.WriteLine("Amqp setup");

            Thread.Sleep(1000);

            // setup AMQP
            Trace.TraceLevel = TraceLevel.Frame | TraceLevel.Information;
            // enable trace
            Trace.TraceListener = WriteTrace;
            Connection.DisableServerCertValidation = false;

            SendLatLongData();
        }

        private async Task SendLatLongData()
        {
            try
            {
                // parse Azure IoT Hub Map settings to AMQP protocol settings
                string hostName = HubName + ".azure-devices.net";
                string userName = DeviceId + "@sas." + HubName;
                string senderAddress = "devices/" + DeviceId + "/messages/events";
                string receiverAddress = "devices/" + DeviceId + "/messages/deviceBound";

                Console.WriteLine("Create connection factory...");
                var factory = new ConnectionFactory();
                Console.WriteLine("Create connection ...");
                var connection = await factory.CreateAsync(new Address(hostName, 5671, userName, SasToken));
                Console.WriteLine("Create session ...");
                var session = new Session(connection);
                Console.WriteLine("Create SenderLink ...");
                var sender = new SenderLink(session, "send-link", senderAddress);
                Console.WriteLine("Create ReceiverLink ...");
                var receiver = new ReceiverLink(session, "receive-link", receiverAddress);
                receiver.Start(100, OnMessage);

                while (true)
                {
                    // update the location data
                    Console.WriteLine("Update location data");

                    UpdateMockDestination();

                    Console.WriteLine("Create payload");
                    string messagePayload = $"{{\"Latitude\":{latitude},\"Longitude\":{longitude}}}";

                    // compose message
                    Console.WriteLine("Create message");
                    var message = new Message(Encoding.UTF8.GetBytes(messagePayload));
                    message.ApplicationProperties = new Amqp.Framing.ApplicationProperties();

                    // send message with the new Lat/Lon
                    Console.WriteLine("Send message");
                    sender.Send(message, null, null);

                    // data sent
                    Console.WriteLine($"*** DATA SENT - Lat - {latitude}, Lon - {longitude} ***");

                    // wait before sending the next position update
                    Console.WriteLine("Sleep");
                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"-- D2C Error - {ex.Message} --");
            }
        }

        private static void OnMessage(IReceiverLink receiver, Message message)
        {
            Console.WriteLine("Message received");

            try
            {   // command received 
                double.TryParse((string)message.ApplicationProperties["setlat"], out latitude);
                double.TryParse((string)message.ApplicationProperties["setlon"], out longitude);
                Console.WriteLine($"== Received new Location setting: Lat - {latitude}, Lon - {longitude} ==");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"-- C2D Error - {ex.Message} --");
            }
        }

        static void WriteTrace(TraceLevel level, string format, params object[] args)
        {
            if (traceOn)
            {
                Console.WriteLine(Fx.Format(format, args));
            }
        }

        // Starting at the last Lat/Lon move along the bearing and for the distance to reset the Lat/Lon at a new point...
        public static void UpdateMockDestination()
        {
            // Get a random Bearing and Distance...
            double distance = rand.Next(10);     // Random distance from 0 to 10km...
            double bearing = rand.Next(360);     // Random bearing from 0 to 360 degrees...

            double lat1 = latitude * (Math.PI / 180);
            double lon1 = longitude * (Math.PI / 180);
            double brng = bearing * (Math.PI / 180);
            double lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(distance / radius) + Math.Cos(lat1) * Math.Sin(distance / radius) * Math.Cos(brng));
            double lon2 = lon1 + Math.Atan2(Math.Sin(brng) * Math.Sin(distance / radius) * Math.Cos(lat1), Math.Cos(distance / radius) - Math.Sin(lat1) * Math.Sin(lat2));

            latitude = lat2 * (180 / Math.PI);
            longitude = lon2 * (180 / Math.PI);
        }
    }
}