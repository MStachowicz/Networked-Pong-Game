using System;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using OpenTK;

namespace PongGame
{
    /// <summary>
    /// Manages outgoing and incoming messages for both UDP and TCP protocol.
    /// </summary>
    class NetworkManager
    {
        /// <summary>
        /// The IP adress of the opponent client the lobby scene found by listening to local network UDP broadcasts.
        /// </summary>
        public IPAddress OpponentIP;
        /// <summary>
        /// The IP adress of this machinefound in the lobby scene.
        /// </summary>
        public IPAddress MyIP;
        /// <summary>
        /// The IP adress of the server the two peers connect to.
        /// </summary>
        public IPAddress ServerIP = IPAddress.Parse("150.237.45.33");


        // HIGHSCORES
        const int HIGHSCORESIZE = 5;
        public string[] highscoreNames = new string[HIGHSCORESIZE];
        public int[] highscoreScores = new int[HIGHSCORESIZE];


        /// <summary>
        /// The port number used for TCP connections.
        /// </summary>
        private int TCPportnumber = 43;
        /// <summary>
        /// The port number used for UDP connections.
        /// </summary>
        private int UDPportNumber = 43;
        /// <summary>
        /// The timeout for the UDP and TCP connections, if set to 0 timeout is turned off. (time in ms)
        /// </summary>
        private int timeout = 0;
        /// <summary>
        /// The scenemanager instance running the game, used to carry out commands interpreted by the UDP
        /// and TCP listeners in the NetworkManager.
        /// </summary>
        private SceneManager sceneManager;

        public bool stopUDPListener = false;
        public bool stopUDPBroadcast = false;
        public bool stopTCPListener = false;
        public bool stopTCPBroadcast = false;

        /// <summary>
        /// The game instance the network manager will control if the gametype is a network game
        /// </summary>
        private GameScene game;

        /// <summary>
        /// Constructor for network manager, finds and sets the IP address of this machine.
        /// </summary>
        /// <param name="pSceneManager">The scenemanager instance whos member this network Manager instance belongs to</param>
        public NetworkManager(SceneManager pSceneManager)
        {
            // Find the local ipv4 adress of the current machine.
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    MyIP = addr;
                }
            }
            logger.Log("Network Manager | My IP has been set to: " + MyIP, logger.MessageType.title);
            sceneManager = pSceneManager;
        }

        /// <summary>
        /// Send message to server to update its highscore table with the parameter string.
        /// </summary>
        /// <param name="highscore"></param>
        public void UpdateServerHighscoreTable(string highscore)
        {
            sceneManager.networkManager.SendTCPMessage(
                sceneManager.networkManager.ServerIP,
                string.Format("UpdateHighScore@{0}", highscore),
                true);
        }

        /// <summary>
        /// Send a retrieve high score message to the server and update the local highscore table.
        /// </summary>
        public void UpdateLocalHighScore()
        {
            // Send a lookup request to retrieve the highscore table from the server
            //client.HandleRequest(new string[] { "/h", "localhost", GameID });

            // sends the server a request to return a highscore table, creates a default table and 
            // sends it if one doesnt exist.
            SendTCPMessage(ServerIP, "RetrieveHighScore", true);




            ////If the highscore was not not found by the retrieve attempt
            //if (ServerResponse == null)
            //{
            //    // Send a default highscore table to update the server with for the Game ID entry
            //    string DefaultHighscoreString = "Darren@5@Dawn@4@David@3@Steven@2@Susan@1";

            //    client.HandleRequest(new string[] { "/h", "localhost", GameID, DefaultHighscoreString });

            //    // TODO add error handling for incorrect highscore file download.


        }

        /// <summary>
        /// Assigns an instance of the game scene as a member variable of the network manager to 
        /// control it when messages are received.
        /// </summary>
        public void AssignGame(GameScene pGame)
        {
            game = pGame;
        }

        #region TCP

        public void SendTCPMessage(IPAddress DestinationIP, string message, bool listenForResponse)
        {
            TcpClient client;
            StreamWriter ServerStreamWriter;
            StreamReader serverResponseStream;

            string serverResponse;
            string[] ServerResponseLines;

            client = new TcpClient();

            try
            {
                client.Connect(DestinationIP.ToString(), TCPportnumber);
                logger.Log(string.Format("Network Manager | Connected to client with IP \"{0}\" on port \"{1}\"", DestinationIP, TCPportnumber),
                    logger.MessageType.general);
            } 
            catch (Exception e)
            {
                logger.Log(string.Format("Error: Failed to connect to the server with IP \"{0}\" on port \"{1}\"", DestinationIP, TCPportnumber), logger.MessageType.error);
                logger.Log(e.ToString(), logger.MessageType.error);
            }

            if (timeout > 0) // if the timeout was set
            {
                client.ReceiveTimeout = timeout;
                client.SendTimeout = timeout;
            }

            // Set up reader and writer.
            ServerStreamWriter = new StreamWriter(client.GetStream());
            serverResponseStream = new StreamReader(client.GetStream());

            // Send the message using the streamwriter.
            ServerStreamWriter.Write(message);
            ServerStreamWriter.Flush();

            logger.Log(string.Format("Network Manager | Sent message \"{0}\" to IP {1}", message, DestinationIP),
                    logger.MessageType.general);

            // Interprets the response of the server to confirm actions.
            if (listenForResponse)
            {
                using (serverResponseStream)
                    try // Try to read the response from the server and seperate it into lines.
                    {
                        serverResponse = serverResponseStream.ReadToEnd();
                        string[] delimiters = new string[] { "@" };
                        ServerResponseLines = serverResponse.Split(delimiters, StringSplitOptions.None);



                        // INTERPRETING SERVER RESPONSE.

                        // MASTER PEER MESSAGE RESPONSES
                        if (ServerResponseLines[0] == "ServerSet")
                        {
                            logger.Log(string.Format("Server received the master peer message to set the master and slave IP"),
                       logger.MessageType.general);
                            game.hasConnected = true;
                        }
                        else if (ServerResponseLines[0] == "startGameSlave")
                        {
                            logger.Log(string.Format(""),
                       logger.MessageType.general);
                            game.hasConnected = true;
                        }
                        // SLAVE PEER MESSAGE RESPOSES
                        if (serverResponse == "startGameSlave") // Server already connected to the master peer. starts game.
                        {
                            logger.Log(string.Format("Server connected to master peer, game starting."),
                      logger.MessageType.general);
                            game.hasConnected = true;
                        }
                        if (serverResponse == "MasterPeerNotConnected")
                        {
                            logger.Log(string.Format("Master peer not connected to the server yet."),
                      logger.MessageType.general);
                        }


                        // HIGHSCORE MESSAGE REPLIES FROM SERVER
                        if (ServerResponseLines[0] == "NoHighscoreFound")
                        {
                            for (int i = 0; i < HIGHSCORESIZE; i++)
                            {
                                highscoreNames[i] = ServerResponseLines[(i * 2) + 1];
                                int.TryParse(ServerResponseLines[(i * 2 + 1) + 1], out highscoreScores[i]);
                            }

                            // Lastly sort the arrays by highest scores ascending.
                            Array.Sort(keys: highscoreScores, items: highscoreNames);
                        }
                        else if (ServerResponseLines[0] == "HighscoreFound")
                        {
                            for (int i = 0; i < HIGHSCORESIZE; i++)
                            {
                                highscoreNames[i] = ServerResponseLines[(i * 2) + 1];
                                int.TryParse(ServerResponseLines[(i * 2 + 1) + 1], out highscoreScores[i]);
                            }

                            // Lastly sort the arrays by highest scores ascending.
                            Array.Sort(keys: highscoreScores, items: highscoreNames);
                        }

                        logger.Log(string.Format("Server response: " + serverResponse),
                       logger.MessageType.general);
                    }
                    catch (Exception e)
                    {
                        logger.Log("Error: Failed to read the stream response from the server.\n" + e.ToString(), logger.MessageType.error);
                    }
            }

            //client.Close();
        }

        /// <summary>
        /// Listens to all incoming TCP messages on the local network. When makes a connection runs the handle request method on a new thread.
        /// Continues running until stopTCPListener is set to true.
        /// </summary>
        /// <param name="IPToListenTo">The specific IP adress to listen for.</param>
        public void ListenForTCPMessage()
        {
            stopTCPListener = false;

            TcpListener listener;
            Socket connection;
            //Handler RequestHandler;

            try
            {
                // USING METHOD
                // Create a TCP socket to listen on port 43 for incoming requests and start listening.
                listener = new TcpListener(IPAddress.Any, TCPportnumber);
                listener.Start();
                logger.Log("Network Manager | started listening for TCP messages on local network port: " + TCPportnumber, logger.MessageType.title);
                // Loop forever handling all incoming requests by creating threads.
                while (!stopTCPListener)
                {
                    // When a request is received create a socket to handle it and invoke doRequest on a new thread to handle the details.
                    connection = listener.AcceptSocket();
                    logger.Log("Network Manager | received a connection from IP: " + connection.LocalEndPoint, logger.MessageType.title);


                    Thread t = new Thread(() => handleRequest(connection));
                    t.Start();
                }



                // USING THE HANDLER CLASS FROM ACW1
                // Create a TCP socket to listen on port 43 for incoming requests.
                // and start listening.
                //listener = new TcpListener(IPAddress.Any, 43);
                //listener.Start();

                //logger.Log("Server started listening", logger.MessageType.debug);

                //// Loop forever handling all incoming requests by creating threads.
                //while (true)
                //{
                //    // When a request is received create a socket to handle it and 
                //    // invoke doRequest on a new thread to handle the details.
                //    connection = listener.AcceptSocket();
                //    RequestHandler = new Handler();
                //    Thread t = new Thread(() => RequestHandler.doRequest(connection));
                //    t.Start();
                //}
            }
            catch (Exception e)
            {
                // If there was an error in processing - catch and log the details.
                logger.Log("Exception " + e.ToString(), logger.MessageType.error);
            }
            finally
            {
                logger.Log("Network Manager | Stopped listening for TCP messages", logger.MessageType.title);
            }
        }
        /// <summary>
        /// Handler class to perform a request on a connection.
        /// </summary>
        class Handler
        {
            /// <summary>
            /// This method is called after the server receives a connection on a listener.
            /// It processes the lines received as a request in the desired protocol and
            /// sends back appropriate reply to the client.
            /// </summary>
            /// <param name="socketStream"></param>
            public void doRequest(Socket connection)
            {
                NetworkStream socketStream;
                socketStream = new NetworkStream(connection);

                try
                {
                    #region Read request sent by client

                    //// Set the timeout value to 1 second
                    //if (false)
                    //{
                    //socketStream.ReadTimeout = timeout;
                    //socketStream.WriteTimeout = timeout;
                    //}

                    // create some stream readers to handle the socket I/O
                    // -Much more convenient than byte arrays
                    // Particularly as the data will be ASCI text structures in lines.
                    StreamReader sr = new StreamReader(socketStream);
                    StreamWriter sw = new StreamWriter(socketStream);

                    // Reads the stream into a string clientMessage until the end of file.
                    string clientMessage = readStream(sr);

                    // Splits the client message by lines to more easily extract information.
                    string[] delimiters = new string[] { "\r\n" };
                    string[] lines = clientMessage.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                    #endregion

                    #region  Interpret the request sent by the client


                    logger.Log(string.Format("Handler class received message from IP: \"{0}\" message: \"{1}\" ", connection.AddressFamily, clientMessage),
                        logger.MessageType.debug);

                    #endregion

                    #region server response to client request

                    //string reply = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n";
                    //sw.WriteLine(reply);
                    //sw.Flush();

                    #endregion
                }
                catch (Exception e)
                {
                    logger.Log(string.Format("Uncaught exception in handler class: {0}", e.ToString()),
                        logger.MessageType.error);
                }
                finally
                {
                    socketStream.Close();
                    connection.Close();
                }
            }

            /// <summary>
            /// Reads the stream into a string char by char until the end of the file.
            /// </summary>
            /// <param name="reader"></param>
            /// Reader to read data from.
            /// <returns></returns>
            public static string readStream(StreamReader reader)
            {
                char[] buffer = new char[1];
                string output = "";
                while (reader.Peek() > -1)
                {
                    reader.Read(buffer, 0, 1);
                    output += buffer[0];
                }
                return output;
            }
        }
        /// <summary>
        /// This method is called after the server receives a connection on a listener.
        /// It processes the lines received as a request in the desired protocol and
        /// sends back appropriate reply to the client.
        /// </summary>
        /// <param name="socketStream"></param>
        public void handleRequest(Socket connection)
        {
            NetworkStream socketStream;
            try
            {
                socketStream = new NetworkStream(connection);

                if (timeout > 0) // if a timeout was set.
                {
                    socketStream.ReadTimeout = timeout;
                    socketStream.WriteTimeout = timeout;
                }

                StreamReader sr = new StreamReader(socketStream);
                StreamWriter sw = new StreamWriter(socketStream);

                // Reads the stream into a string clientMessage until the end of file.
                string clientMessage = readStream(sr);
                logger.Log(string.Format("Network Manager | Read TCP message: \"{0}\" From IP \"{1}\"", clientMessage, connection.LocalEndPoint), logger.MessageType.title);

                // Splits the client message by lines to more easily extract information.
                string[] delimiters = new string[] { "@" };
                string[] lines = clientMessage.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                #region Interpreting message

                if (clientMessage.StartsWith("locationSet"))
                {
                    if (game.MasterPeer) // master peer controls paddle 1 so network sets 2 position
                    {
                        game.SetPaddle2Position(new Vector2(SceneManager.WindowWidth - 40, float.Parse(lines[2])));
                    }
                    else
                    {
                        game.SetPaddle1Position(new Vector2(SceneManager.WindowWidth - 40, float.Parse(lines[2])));
                    }
                }
                else if (clientMessage.StartsWith("GameReset"))
                {
                    if (!game.MasterPeer)
                    {
                        logger.Log("Network Manager received request to reset game. Game is the slave peer, resetting", logger.MessageType.general);
                        game.ResetGame();
                        game.SetBallVelocity(new Vector2(float.Parse(lines[1]), float.Parse(lines[2])));
                    }
                    else
                    {
                        logger.Log("Network Manager received request to reset game. Game is the master peer, game not reset", logger.MessageType.general);

                    }
                }
                else if (clientMessage.StartsWith("SyncGame"))
                {
                    if (!game.MasterPeer)
                    {
                        logger.Log("Network Manager received request to sync game. Game is the slave peer, syncing", logger.MessageType.general);
                    }
                    else
                    {
                        logger.Log("Network Manager received request to sync game. Game is the master peer, not syncing", logger.MessageType.general);
                    }
                }
                else if (clientMessage == "connected")
                {
                    logger.Log("Network Manager received connection confirmation from the slave peer at IP:  " + connection.LocalEndPoint,
                       logger.MessageType.debug);
                    game.hasConnected = true;
                }
                else
                {
                    logger.Log("Network Manager could not interpret message:  " + clientMessage + " From IP: " + connection.LocalEndPoint,
                        logger.MessageType.error);
                }

                #endregion

            }
            catch (Exception e)
            {
                logger.Log("Error occurred in Network Manager\nException:\n " + e.ToString(), logger.MessageType.error);
            }
            finally
            {
                //socketStream.Close();
                //connection.Close();
            }
        }



        /// <summary>
        /// Reads the stream into a string char by char until the end of the file.
        /// </summary>
        /// <param name="reader"></param>
        /// Reader to read data from.
        /// <returns></returns>
        private string readStream(StreamReader reader)
        {
            char[] buffer = new char[1];
            string output = "";
            while (reader.Peek() > -1)
            {
                reader.Read(buffer, 0, 1);
                output += buffer[0];
            }
            return output;
        }

        #endregion

        #region UDP

        public void startUDPBroadcast(string broadcastMessage)
        {
            // set switch to false so the broadcast continues until it is switched off elsewhere
            stopUDPBroadcast = false;

            logger.Log("Network Manager | started broadcasting UDP message on local network: " + broadcastMessage, logger.MessageType.title);
            UdpClient client = new UdpClient();
            IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, UDPportNumber);

            while (!stopUDPBroadcast)
            {
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(broadcastMessage);
                client.Send(bytes, bytes.Length, ip);
            }
            client.Close();
            logger.Log("Network Manager | stopped broadcasting UDP message on local network: " + broadcastMessage, logger.MessageType.title);
        }

        /// <summary>
        /// Listens for UDP messages on the local network from all IP addresses. Starts a networked game when reads the correct message.
        /// </summary>
        /// <param name="isMasterPeer"></param>
        public void listenforUDPBroadcasts(bool isMasterPeer)
        {
            stopUDPListener = false;

            UdpClient listener = new UdpClient(UDPportNumber);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, UDPportNumber);

            logger.Log("Network Manager | started listening for UDP broadcast on local network on port: " + UDPportNumber, logger.MessageType.title);

            try
            {
                while (!stopUDPListener)
                {
                    byte[] receivedBytes = listener.Receive(ref groupEP);
                    string receivedString = System.Text.Encoding.ASCII.GetString(receivedBytes, 0, receivedBytes.Length);

                    logger.Log(string.Format("Network Manager | read UDP message: \"{0}\" From IP: \"{1}\"", receivedString, groupEP.Address),
                    logger.MessageType.title);

                    if (isMasterPeer) // If this game is the master peer
                    {
                        // IP based message interpetation for setting up a networked game
                        IPAddress.TryParse(receivedString, out OpponentIP);

                        // If the IP was parsed successfully and does not orginate from the current machine
                        // stop sending broadcast and listening for one
                        if (OpponentIP == null)
                        {
                            logger.Log("Network Manager | Message received was not a valid IP adress", logger.MessageType.error);
                        }
                        else if (OpponentIP.Equals(MyIP))
                        {
                            //logger.Log("Broadcast received was from this machine", logger.MessageType.error);
                            continue;
                        }
                        else // Ip adresss of other player has been found
                        {
                            // Correctly parsed IP address that is not this machine's is used to connect.
                            logger.Log("Network Manager | Connecting to player with address - " + OpponentIP + " - Syncing and starting the networked game ",
                                logger.MessageType.gameChange);
                            stopUDPListener = true;
                            stopUDPBroadcast = true;
                            sceneManager.StartNewGame(GameScene.GameType.Network, true); // start new game as master peer
                            break;
                        }
                    }
                    else
                    {
                        // Checks if the message received is from a master peer attempting to connect to this game. 
                        if (receivedString == MyIP + "@StartGame")
                        {
                            // Slave peer waits to receive this message to start a new game.
                            logger.Log("Network Manager | Received prompt to start the game from the master peer at IP: " + groupEP.Address, logger.MessageType.debug);
                            stopUDPListener = true;
                            stopUDPBroadcast = true;
                            OpponentIP = groupEP.Address;
                            sceneManager.StartNewGame(GameScene.GameType.Network, false); // start new game as slave peer
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log(e.ToString(), logger.MessageType.error);
            }
            finally
            {
                listener.Close();
                logger.Log("Network Manager | stopped listening for UDP broadcast on local network", logger.MessageType.title);
            }
        }

        #endregion
    }
}