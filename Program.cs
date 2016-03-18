using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IrcDotNet;
using System.Threading;
using MarkovBot;
using System.Security.Cryptography;
using System.Globalization;

namespace MarkovIRC
{
    class Program
    {
        static RNGCryptoServiceProvider RNG = new RNGCryptoServiceProvider();

        static string DoCapitalization(String Input)
        {
            string[] Parts = Input.Split('.');
            for (int x = 0; x < Parts.Length; x++)
            {
                Parts[x] = Parts[x].ToLower();
                int Y = Parts[x].IndexOf(S => char.IsLetterOrDigit(S));

                if (Y > -1)
                    Parts[x] = Parts[x].Substring(0, Y) + Char.ToUpper(Parts[x][Y]) + Parts[x].Substring(Y + 1);
            }

            return String.Join(".", Parts);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Loading Brain");
            Markov Markov = new Markov();
            Markov.LoadBrain("BRAIN");
            Console.WriteLine("Brain Loaded.  Auditing...");

            if (!Markov.Audit())
            {
                throw new InvalidOperationException("Audit Failed");
            }

            Console.WriteLine("Audit Passed.  Connecting to server...");

            IrcClient Client = new IrcClient();

            // *** MAIN PROCESSING
            EventHandler<IrcMessageEventArgs> Replier = delegate(Object Sender, IrcMessageEventArgs Event)
            {
                IrcChannel Channel = (IrcChannel)Sender;

                TextInfo TI = new CultureInfo("en-CA", false).TextInfo;

                if (Event.Text.ToLower() == "!exit" && Event.Source.Name.ToLower() == "sukasa")
                {
                    Client.Quit("*DEAD*");
                    Client.Disconnect();
                }
                else
                {
                    Byte[] Value = new Byte[1];
                    RNG.GetBytes(Value);
                    if ((Value[0] >= 245 || Event.Text.ToLower().Contains("kbot")))
                    {
                        String Reply = DoCapitalization(Markov.GenerateReply(Event.Text));
                        Client.LocalUser.SendMessage(Channel.Name, Reply);
                    }
                    Markov.Learn(Event.Text);
                }
            };

            IrcUserRegistrationInfo RegInfo = new IrcUserRegistrationInfo();
            RegInfo.NickName = "kBot";
            RegInfo.UserName = "KasaMarkovBot";
            RegInfo.RealName = "4th order Markov chain bot";

            Uri ServerAddress = new Uri("irc://irc.irchighway.net:6667");

            using (var connectedEvent = new ManualResetEventSlim(false))
            {
                Client.Connected += (sender2, e2) => connectedEvent.Set();
                Client.Connect(ServerAddress, RegInfo);

                if (!connectedEvent.Wait(10000))
                {
                    Client.Dispose();
                    Console.WriteLine("Connection to {0} timed out.", ServerAddress);
                    return;
                }
                
                Console.WriteLine("Connected to {0}.", ServerAddress);

                // *** POST-INIT
                Client.MotdReceived += delegate(Object Sender, EventArgs E)
                {
                    Console.WriteLine("Joining Channels...");
                    Client.Channels.Join("#la-mulana");
                };

                // *** DEBUG OUTPUT
                Client.RawMessageReceived += delegate(Object Sender, IrcRawMessageEventArgs Event)
                {
                    Console.WriteLine(Event.RawContent);
                };

                // *** PING
                Client.PingReceived += delegate(Object Sender, IrcPingOrPongReceivedEventArgs Event)
                {
                    Console.WriteLine("Ping Pong");
                    Client.Ping(Event.Server);
                };

                // *** CHANNEL JOINING
                Client.LocalUser.JoinedChannel += delegate(Object Sender, IrcChannelEventArgs Event)
                {
                    Event.Channel.MessageReceived += Replier;
                };

                // *** MESSAGE ECHOING
                Client.RawMessageSent += delegate(Object Sender, IrcRawMessageEventArgs Event)
                {
                    Console.WriteLine(Event.RawContent);
                };

                // *** ERROR REPORTING
                Client.Error += delegate(Object Sender, IrcErrorEventArgs Event)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Event.Error.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                };

                // *** ERROR REPORTING
                Client.ErrorMessageReceived += delegate(Object Sender, IrcErrorMessageEventArgs Event)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Event.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                };

                // *** ERROR REPORTING
                Client.ProtocolError += delegate(Object Sender, IrcProtocolErrorEventArgs Event)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Event.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                };

                // *** REJOIN AFTER KICK
                Client.LocalUser.LeftChannel += delegate(Object Sender, IrcChannelEventArgs Event)
                {
                    Client.Channels.Join(Event.Channel.Name);
                };

                Int32 Counter = 0;
                while (Client.IsConnected)
                {
                    Thread.Sleep(5);
                    if (++Counter == 12000)
                    {
                        Console.Write("Manual Ping ");
                        Client.Ping();
                        Counter = 0;
                    }
                    while (Console.KeyAvailable)
                    {
                        if (Console.ReadKey().Key == ConsoleKey.Delete)
                            Client.Disconnect();

                    }
                }

                Console.WriteLine("Saving Brain");
                Markov.SaveBrain("BRAIN");
            }
        }

    }
}
