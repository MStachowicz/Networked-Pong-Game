using System;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using System.Timers;
using System.Collections.Generic;

namespace PongGame
{
    class GameScene : Scene, IScene
    {
        Matrix4 projectionMatrix;

        PlayerPaddle paddlePlayer1;
        PlayerPaddle paddlePlayer2;
        AIPaddle paddleAI;

        Ball ball;

        Random rand = new Random();

        enum PowerUpType
        {
            DoublePaddle, halfPaddle, stickyPaddle
        };

        /// <summary>
        /// List storing all the instances of the power up class
        /// </summary>
        List<PowerUp> poweruplist = new List<PowerUp>();
        struct PowerUp
        {
            public PowerUp(PowerUpType pType)
            {
                powerUpType = pType;
                ball = new Ball((int)(SceneManager.WindowWidth * 0.5), (int)(SceneManager.WindowHeight * 0.5));
                ball.ballType = Ball.BallType.powerup;

                if (pType == PowerUpType.DoublePaddle)
                {
                    ball.Colour = new Vector3(0.0f, 1.0f, 0.0f);
                }
                else if (pType == PowerUpType.halfPaddle)
                {
                    ball.Colour = new Vector3(1.0f, 0.0f, 0.0f);
                }
                else if (pType == PowerUpType.stickyPaddle)
                {
                    ball.Colour = new Vector3(0.0f, 0.0f, 1.0f);
                }

                ball.Init();
                isCollected = false;
            }

            public PowerUpType powerUpType;
            public Ball ball;
            public bool isCollected;
        };


        private void createPowerUp()
        {
            // Select a random power up type and create it.
            int PowerUpSelect = rand.Next(1, 4);

            if (PowerUpSelect == 1)
            {
                poweruplist.Add(new PowerUp(PowerUpType.DoublePaddle));
            }
            else if (PowerUpSelect == 2)
            {
                poweruplist.Add(new PowerUp(PowerUpType.halfPaddle));
            }
            else if (PowerUpSelect == 3)
            {
                poweruplist.Add(new PowerUp(PowerUpType.stickyPaddle));
            }
        }



        // Is this instance of game the master that sends all the game changes to the slave to update.
        public bool MasterPeer;

        /// <summary>
        /// Has the game made a connection to the opponent for a networked game
        /// </summary>
        public bool hasConnected = false;

        private int PaddleMoveSpeed = 7;

        int scorePlayer = 0;
        int scoreOpponent = 0;

        public enum GameType { SinglePlayer, Multiplayer, Network }
        private GameType gameType;

        // Dictates how functions will perform their actions based on the state the game is in.
        public enum GameState { InGame, ResettingGame, Paused }
        /// <summary>
        /// The state the game is currently in.
        /// </summary>
        public GameState gamestate = GameState.ResettingGame;

        /// <summary>
        /// How long the game will last
        /// </summary>
        int GameTimer = 30;
        /// <summary>
        /// Timer object which will handle the time interval for game. Executes the Timer Tick method 
        /// every in game second.
        /// </summary>
        private System.Timers.Timer Timer;

        /// <summary>
        /// Constructor for the game scene, if the gametype is a network game starts a server to listen 
        /// for connection from opponent. 
        /// </summary>
        /// <param name="sceneManager"></param>
        /// <param name="pGameType"></param>
        public GameScene(SceneManager sceneManager, GameType pGameType, bool isMasterPeer) : base(sceneManager)
        {
            // Set the title of the window
            sceneManager.Title = "Pong - " + pGameType.ToString();

            // Set the Render and Update delegates to the Update and Render methods of this class
            sceneManager.renderer = Render;
            sceneManager.updater = Update;

            // Set Keyboard events to go to a method in this class
            sceneManager.Keyboard.KeyDown += Keyboard_KeyDown;
            sceneManager.Keyboard.KeyUp += Keyboard_KeyUp;
            sceneManager.Keyboard.KeyRepeat = true; // allows smooth movement by holding down key.

            // Set Keyboard events to go to a method in this class
            sceneManager.Mouse.ButtonDown += Mouse_ButtonDown;

            gameType = pGameType;


            // Setting up timer
            Timer = new System.Timers.Timer(interval: 1000);
            Timer.Elapsed += timerTick;


            if (gameType == GameType.Network)
            {
                // Pass the network manager this game scene instance so it can control the game
                // when receiving the appropriate messages
                sceneManager.networkManager.AssignGame(this);
                MasterPeer = isMasterPeer;

                if (MasterPeer)
                {
                    // If this game is the master peer begins a UDP broadcast telling opponent to start the game
                    Thread UDPBroadcastThread = new Thread(() =>
                    sceneManager.networkManager.startUDPBroadcast(sceneManager.networkManager.OpponentIP.ToString() + "@StartGame"));
                    UDPBroadcastThread.Start();

                    // All games will connect to the server.

                    // Master peer begins waiting to receive the "connected" TCP message from slave peer.
                    //Thread TCPListenThread = new Thread(() =>
                    //sceneManager.networkManager.ListenForTCPMessage());
                    //TCPListenThread.Start();

                    // Assigns this IP as the master peer in the server.
                    sceneManager.networkManager.SendTCPMessage(
                        sceneManager.networkManager.OpponentIP,
                        string.Format("{0}@{1}@MasterPeer", sceneManager.networkManager.MyIP, sceneManager.networkManager.OpponentIP),
                        true);

                    logger.Log("Master peer waiting for server to signal start of game.", logger.MessageType.gameChange);

                    while (true) // stuck in loop until the slave peer connects.
                    {
                        if (hasConnected) // game is only started once a connection is made to the opponent.
                        {
                            Timer.Start();
                            ResetGame(); // reset game sends a tcp message to slave peer to reset their game.
                            break;
                        }
                    }

                }
                else
                {
                    // slave peer begins listening for messages from master peer.
                    //Thread TCPListenThread = new Thread(() =>
                    //sceneManager.networkManager.ListenForTCPMessage());
                    //TCPListenThread.Start();


                    // Keep connecting to the server waiting for it to reply game start
                    while (true)
                    {
                        try
                        {
                            // Sending message to master peer to set its hasConnected status to true and begin reseting.
                            sceneManager.networkManager.SendTCPMessage(
                                sceneManager.networkManager.MyIP, // slave peer is running the server
                                "connected",
                                true);
                        }
                        catch
                        {
                            logger.Log(
                                string.Format("Slave peer retrying connection to the server on IP " + sceneManager.networkManager.MyIP),
                                logger.MessageType.debug);
                        }

                        if (hasConnected) // game is only started once a connection is made to the opponent.
                        {
                            Timer.Start();
                            ResetGame(); // reset game sends a tcp message to slave peer to reset their game.
                            break;
                        }
                    }


                    // Exectution passes here once the tcp message is sent
                    //hasConnected = true; 
                }
            }
            else // for non-networked games.
            {
                Timer.Start();
                ResetGame();
            }

            GL.ClearColor(Color.Black);
        }

        Thread SendThread;

        #region Input

        public void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            switch (gameType)
            {
                case GameType.SinglePlayer:
                    switch (e.Key)
                    {
                        case Key.W:
                            paddlePlayer1.moveUp = true;
                            break;
                        case Key.S:
                            paddlePlayer1.moveDown = true;
                            break;
                        case Key.Space:
                            if (paddlePlayer1.ballStuckToPaddle != null)
                            {
                                paddlePlayer1.sticky = false;
                                paddlePlayer1.ballStuckToPaddle.stuck = false;
                                paddlePlayer1.ballStuckToPaddle = null;
                            }
                            break;
                    }

                    break;
                case GameType.Multiplayer:
                    switch (e.Key)
                    {

                        // PLAYER 1
                        case Key.W:
                            paddlePlayer1.moveUp = true;
                            break;
                        case Key.S:
                            paddlePlayer1.moveDown = true;
                            break;
                        case Key.Space:
                            if (paddlePlayer1.ballStuckToPaddle != null)
                            {
                                paddlePlayer1.sticky = false;
                                paddlePlayer1.ballStuckToPaddle.stuck = false;
                                paddlePlayer1.ballStuckToPaddle = null;
                            }
                            break;


                        // PLAYER 2
                        case Key.Up:
                            paddlePlayer2.moveUp = true;
                            break;
                        case Key.Down:
                            paddlePlayer2.moveDown = true;
                            break;
                        case Key.ControlRight:
                            if (paddlePlayer2.ballStuckToPaddle != null)
                            {
                                paddlePlayer2.sticky = false;
                                paddlePlayer2.ballStuckToPaddle.stuck = false;
                                paddlePlayer2.ballStuckToPaddle = null;
                            }
                            break;



                    }
                    break;

                case GameType.Network:
                    if (MasterPeer) // master peer moves the left paddle
                        switch (e.Key)
                        {
                            case Key.Up:
                                paddlePlayer1.Move(PaddleMoveSpeed);
                                SendThread.Start();
                                break;
                            case Key.Down:
                                paddlePlayer1.Move(-PaddleMoveSpeed);
                                SendThread.Start();
                                break;
                            default:
                                break;
                        }
                    else // slave peer sends its paddle position change to master peer
                        switch (e.Key)
                        {
                            case Key.Up:
                                paddlePlayer2.Move(PaddleMoveSpeed);
                                SendThread.Start();
                                break;
                            case Key.Down:
                                paddlePlayer2.Move(-PaddleMoveSpeed);
                                SendThread.Start();
                                break;
                            default:
                                break;
                        }
                    break;
            }

            // Debugging Keys for all game types
            if (e.Key == Key.T)
            {
                GameTimer += 5;
            }
            if (e.Key == Key.R)
            {
                scorePlayer++;
            }
            if (e.Key == Key.Y)
            {
                scoreOpponent++;
            }
            if (e.Key == Key.Space)
            {
                createPowerUp();
            }

        }

        public void Keyboard_KeyUp(object sender, KeyboardKeyEventArgs e)
        {
            switch (gameType)
            {
                case GameType.SinglePlayer:
                    switch (e.Key)
                    {
                        case Key.W:
                            paddlePlayer1.moveUp = false;
                            //paddlePlayer1.Move(PaddleMoveSpeed);
                            break;
                        case Key.S:
                            paddlePlayer1.moveDown = false;
                            //paddlePlayer1.Move(-PaddleMoveSpeed);
                            break;
                    }
                    break;


                case GameType.Multiplayer:
                    switch (e.Key)
                    {
                        case Key.W:
                            paddlePlayer1.moveUp = false;
                            //paddlePlayer1.Move(PaddleMoveSpeed);
                            break;
                        case Key.S:
                            paddlePlayer1.moveDown = false;
                            //paddlePlayer1.Move(-PaddleMoveSpeed);
                            break;

                        case Key.Up:
                            paddlePlayer2.moveUp = false;
                            break;
                        case Key.Down:
                            paddlePlayer2.moveDown = false;
                            break;
                    }
                    break;





                case GameType.Network:
                    break;
                default:
                    break;
            }
        }

        private void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        {
            // debugging keys
            switch (e.Button)
            {
                case MouseButton.Left:
                    ResetGame();
                    break;
                case MouseButton.Right:
                    Timer.Stop();

                    sceneManager.Keyboard.KeyDown -= Keyboard_KeyDown;
                    sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                    sceneManager.Keyboard.KeyUp -= Keyboard_KeyUp;

                    sceneManager.GameOverMenu(scorePlayer);
                    break;
            }
        }
        #endregion

        #region Scene behaviours

        /// <summary>
        /// Checks if either paddle has collided with the parameter ball
        /// </summary>
        private void CollisionDetection(Ball pBall)
        {
            switch (gameType)
            {
                case GameType.SinglePlayer:
                    if (paddleAI.hasCollidedWithBall(ball, Paddle.PaddleSide.right))
                    {
                        pBall.Position = new Vector2(paddleAI.Position.X - pBall.Radius, pBall.Position.Y);
                        pBall.Velocity = new Vector2(pBall.Velocity.X * -1.0f, pBall.Velocity.Y) * 2.0f;
                    }
                    else if (paddlePlayer1.hasCollidedWithBall(ball, Paddle.PaddleSide.left))
                    {
                        pBall.Position = new Vector2(paddlePlayer1.Position.X + pBall.Radius, pBall.Position.Y);
                        pBall.Velocity = new Vector2(pBall.Velocity.X * -1.0f, pBall.Velocity.Y) * 2.0f;
                    }

                    checkPowerUpCollisions(paddleAI, paddlePlayer1);
                    break;


                case GameType.Multiplayer:
                    if (paddlePlayer2.hasCollidedWithBall(ball, Paddle.PaddleSide.right))
                    {
                        pBall.Position = new Vector2(paddlePlayer2.Position.X - pBall.Radius, pBall.Position.Y);
                        pBall.Velocity = new Vector2(pBall.Velocity.X * -1.0f, pBall.Velocity.Y) * 2.0f;
                    }
                    else if (paddlePlayer1.hasCollidedWithBall(ball, Paddle.PaddleSide.left))
                    {
                        pBall.Position = new Vector2(paddlePlayer1.Position.X + pBall.Radius, pBall.Position.Y);
                        pBall.Velocity = new Vector2(pBall.Velocity.X * -1.0f, pBall.Velocity.Y) * 2.0f;
                    }


                    checkPowerUpCollisions(paddlePlayer2, paddlePlayer1);
                    break;


                case GameType.Network:
                    if (paddlePlayer2.hasCollidedWithBall(ball, Paddle.PaddleSide.right))
                    {
                        pBall.Position = new Vector2(paddlePlayer2.Position.X - pBall.Radius, pBall.Position.Y);
                        pBall.Velocity = new Vector2(pBall.Velocity.X * -1.0f, pBall.Velocity.Y) * 2.0f;
                    }
                    else if (paddlePlayer1.hasCollidedWithBall(ball, Paddle.PaddleSide.left))
                    {

                        pBall.Position = new Vector2(paddlePlayer1.Position.X + pBall.Radius, pBall.Position.Y);
                        pBall.Velocity = new Vector2(pBall.Velocity.X * -1.0f, pBall.Velocity.Y) * 2.0f;
                    }

                    checkPowerUpCollisions(paddlePlayer2, paddlePlayer1);
                    break;

                default:
                    break;
            }
        }

        public void checkPowerUpCollisions(Paddle pRightPaddle, Paddle pLeftPaddle)
        {
            for (int i = 0; i < poweruplist.Count; i++)
            {
                if (pRightPaddle.hasCollidedWithBall(poweruplist[i].ball, Paddle.PaddleSide.right))
                {
                    switch (poweruplist[i].powerUpType)
                    {
                        case PowerUpType.DoublePaddle:
                            poweruplist.RemoveAt(i);
                            pRightPaddle.mScale.Y = 2;
                            break;

                        case PowerUpType.halfPaddle:
                            poweruplist.RemoveAt(i);
                            pRightPaddle.mScale.Y = 0.5f;
                            break;

                        case PowerUpType.stickyPaddle:
                            if (pRightPaddle is AIPaddle)
                            {
                                poweruplist.RemoveAt(i);
                            }
                            else
                            {
                                poweruplist.RemoveAt(i);
                                pRightPaddle.sticky = true;
                            }
                            break;


                        default:
                            break;
                    }
                }
                else if (pLeftPaddle.hasCollidedWithBall(poweruplist[i].ball, Paddle.PaddleSide.left))
                {
                    switch (poweruplist[i].powerUpType)
                    {
                        case PowerUpType.DoublePaddle:
                            poweruplist.RemoveAt(i);
                            pLeftPaddle.mScale.Y = 2;
                            break;

                        case PowerUpType.halfPaddle:
                            poweruplist.RemoveAt(i);
                            pLeftPaddle.mScale.Y = 0.5f;
                            break;

                        case PowerUpType.stickyPaddle:
                            poweruplist.RemoveAt(i);
                            pLeftPaddle.sticky = true;
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true when the ball has passed the edges of screen.
        /// </summary>
        /// <returns></returns>
        private bool GoalDetection()
        {
            if (ball.Position.X < 0)
            {
                logger.Log("Opponent scored", logger.MessageType.title);
                scoreOpponent++;
                return true;
            }
            else if (ball.Position.X > SceneManager.WindowWidth)
            {
                logger.Log("player scored", logger.MessageType.title);
                scorePlayer++;
                return true;
            }

            // Checks if any of the power up balls have left the screen and removes them.
            for (int i = 0; i < poweruplist.Count; i++)
            {
                if (poweruplist[i].ball.Position.X < 0 || poweruplist[i].ball.Position.X > SceneManager.WindowWidth)
                {
                    poweruplist.RemoveAt(i);
                }
            }

            return false;
        }

        /// <summary>
        /// Resets the game, the reset for network game is handled here by synbcing both games
        /// </summary>
        public void ResetGame()
        {
            gamestate = GameState.ResettingGame;

            paddlePlayer1 = new PlayerPaddle(40, (int)(SceneManager.WindowHeight * 0.5));
            paddlePlayer1.Init();

            switch (gameType)
            {
                case GameType.SinglePlayer:
                    paddleAI = new AIPaddle(SceneManager.WindowWidth - 40, (int)(SceneManager.WindowHeight * 0.5));
                    paddleAI.Init();
                    break;
                case GameType.Multiplayer:
                    paddlePlayer2 = new PlayerPaddle(SceneManager.WindowWidth - 40, (int)(SceneManager.WindowHeight * 0.5));
                    paddlePlayer2.Init();
                    break;
                case GameType.Network:
                    // Add syncing functionality to this case.
                    paddlePlayer2 = new PlayerPaddle(SceneManager.WindowWidth - 40, (int)(SceneManager.WindowHeight * 0.5));
                    paddlePlayer2.Init();
                    break;
            }

            ball = new Ball((int)(SceneManager.WindowWidth * 0.5), (int)(SceneManager.WindowHeight * 0.5));
            ball.Init();

            // Removes all the power ups when the game is reset.
            poweruplist.RemoveRange(0, poweruplist.Count);

            // If this game is the master peer send the request to reset game to the opponent game 
            // includes the random ball velocity set by the master peer.
            if (MasterPeer)
            {
                sceneManager.networkManager.SendTCPMessage(sceneManager.networkManager.OpponentIP,
                    string.Format("GameReset@{0}@{1}", ball.Velocity.X, ball.Velocity.Y),
                    false);
            }

            gamestate = GameState.InGame;
        }
        private void timerTick(object sender, ElapsedEventArgs e)
        {
            if (GameTimer > 0)
            {
                GameTimer -= 1;
            }
            else // If the user ran out of time
            {
                Timer.Stop();

                sceneManager.Keyboard.KeyDown -= Keyboard_KeyDown;
                sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                sceneManager.Keyboard.KeyUp -= Keyboard_KeyUp;

                if (gameType == GameType.SinglePlayer)
                {
                    sceneManager.GameOverMenu(scorePlayer);
                }
                else if (gameType == GameType.Multiplayer)
                {
                    sceneManager.MultiplayerGameOverMenu(scorePlayer, scoreOpponent);
                }
            }
        }

        /// <summary>
        /// Sets the ball velocity in the game instance. Used to set the velocity in a slave peer networked game.
        /// </summary>
        /// <param name="pVelocity"></param>
        public void SetBallVelocity(Vector2 pVelocity)
        {
            ball.Velocity = pVelocity;
            logger.Log("ball velocity set to: " + ball.Velocity, logger.MessageType.gameChange);
        }
        /// <summary>
        /// Sends the current location of the player to the opponent game to update. Uses the client class to send the request to the ip found 
        /// in the lobbyScene.
        /// </summary>
        private void sendPaddle1Position()
        {
            sceneManager.networkManager.SendTCPMessage(sceneManager.networkManager.OpponentIP,
                    string.Format("locationSet@{0}@{1}", paddlePlayer1.Position.X, paddlePlayer1.Position.Y),
                    false);
        }
        private void sendPaddle2Position()
        {
            sceneManager.networkManager.SendTCPMessage(sceneManager.networkManager.OpponentIP,
                    string.Format("locationSet@{0}@{1}", paddlePlayer2.Position.X, paddlePlayer2.Position.Y),
                    false);
        }
        public void SetPaddle2Position(Vector2 newPosition)
        {
            paddlePlayer2.Position = newPosition;

            logger.Log("Paddle 2 position set to new position: " + newPosition, logger.MessageType.gameChange);
        }
        public void SetPaddle1Position(Vector2 newPosition)
        {
            paddlePlayer1.Position = newPosition;

            logger.Log("Paddle 1 position set to new position: " + newPosition, logger.MessageType.gameChange);
        }

        #endregion

        #region Update + Render
        public void Update(FrameEventArgs e)
        {
            // Game only updates when the gameState is set to InGame
            if (gamestate == GameState.InGame)
            {
                switch (gameType)
                {
                    case GameType.SinglePlayer:
                        paddleAI.Move(ball.Position);
                        ball.Update((float)e.Time);
                        paddleAI.Update((float)e.Time);
                        paddlePlayer1.Update((float)e.Time);

                        break;
                    case GameType.Multiplayer: // only the ball is updated as both the paddles movements are handled by input methods.
                        paddlePlayer1.Update((float)e.Time);
                        paddlePlayer2.Update((float)e.Time);
                        ball.Update((float)e.Time);
                        break;
                    case GameType.Network: // only the ball is updated as both the paddles movements are handled by input methods.
                        ball.Update((float)e.Time);
                        break;
                }

                // Update the position of all the power up balls
                for (int i = 0; i < poweruplist.Count; i++)
                {
                    poweruplist[i].ball.Update((float)e.Time);
                }

                if (rand.Next(0, 1000) < 8) // chance of power up spawn
                {
                    createPowerUp();
                }

                CollisionDetection(ball);

                if (GoalDetection())
                {
                    ResetGame();
                }
            }
        }

        public void Render(FrameEventArgs e)
        {
            if (gamestate == GameState.InGame)
            {
                GL.Viewport(0, 0, sceneManager.Width, sceneManager.Height);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0, sceneManager.Width, 0, sceneManager.Height, -1, 1);

                GUI.clearColour = Color.Black;


                projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, sceneManager.Width, 0, sceneManager.Height, -1.0f, +1.0f);

                switch (gameType)
                {
                    case GameType.SinglePlayer:
                        ball.Render(projectionMatrix);
                        paddlePlayer1.Render(projectionMatrix);
                        paddleAI.Render(projectionMatrix);
                        break;
                    case GameType.Multiplayer:
                        ball.Render(projectionMatrix);
                        paddlePlayer1.Render(projectionMatrix);
                        paddlePlayer2.Render(projectionMatrix);
                        break;
                    case GameType.Network:
                        ball.Render(projectionMatrix);
                        paddlePlayer1.Render(projectionMatrix);
                        paddlePlayer2.Render(projectionMatrix);
                        break;
                }

                for (int i = 0; i < poweruplist.Count; i++)
                {
                    poweruplist[i].ball.Render(projectionMatrix);
                }



                float width = sceneManager.Width, height = sceneManager.Height, fontSize = Math.Min(width, height) / 20f;

                GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 1, (int)width, (int)(fontSize * 2f)),
                    scorePlayer + " : " + scoreOpponent, (int)fontSize, StringAlignment.Center);
                GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 3, (int)width, (int)(fontSize * 2f)),
                   GameTimer.ToString(), (int)fontSize / 2, StringAlignment.Center);

                if (gameType == GameType.Network)
                {
                    GUI.Label(new Rectangle((int)paddlePlayer1.Position.X, (int)paddlePlayer1.Position.Y, 10, 10),
                       "1", (int)fontSize / 2, StringAlignment.Center);

                    GUI.Label(new Rectangle((int)ball.Position.X, (int)ball.Position.Y, 10, 10),
                       "BALL", (int)fontSize / 2, StringAlignment.Center);

                    GUI.Label(new Rectangle((int)paddlePlayer2.Position.X, (int)paddlePlayer2.Position.Y, 10, 10),
                       "2", (int)fontSize / 2, StringAlignment.Center);
                }

                GUI.Render();
            }
        }
    }
    #endregion
}