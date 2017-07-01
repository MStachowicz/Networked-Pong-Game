using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;

namespace PongGame
{
    class GameOverScene : Scene, IScene
    {
        struct player
        {
            /// <summary>
            /// The string to store the name of the player when it is being inputted if a new highscore is set.
            /// </summary>
            public string name;
            /// <summary>
            /// The score of the player when the game ended.
            /// </summary>
            public int score;
            /// <summary>
            /// The position in the highscores array the player will assume if a highscore is being set.
            /// </summary>
            public int highscorePosition;
            /// <summary>
            /// Whether the player has set a new high score, set to true or false by the checkScore method in the scene constructor.
            /// </summary>
            public bool hasSetHighScore;
            /// <summary>
            /// If the highscore if one has been set by the player, has already been set by the player typing in their name and hitting enter.
            /// </summary>
            public bool HasHighScoreAdded;
        }

        /// <summary>
        /// Called at the start of the constructor to reset the scene after previous games.
        /// </summary>
        public void ResetScene()
        {
            player1.name = null;
            player1.score = 0;
            player1.highscorePosition = 0;
            player1.hasSetHighScore = false;
            player1.HasHighScoreAdded = false;

            player2.name = null;
            player2.score = 0;
            player2.highscorePosition = 0;
            player2.hasSetHighScore = false;
            player2.HasHighScoreAdded = false;
        }


        player player1 = new player();
        player player2;

        /// <summary>
        /// Used to determine how many player scores need to checked before proceeding to the high score scene.
        /// </summary>
        GameScene.GameType gametype;

        public GameOverScene(SceneManager sceneManager, int pPlayerScore, GameScene.GameType pGameType) : base(sceneManager)
        {
            ResetScene();

            // Set the title of the window
            sceneManager.Title = "Pong - Game Over";

            // Set the Render and Update delegates to the Update and Render methods of this class
            sceneManager.renderer = Render;
            sceneManager.updater = Update;

            player1.score = pPlayerScore;
            gametype = pGameType;

            sceneManager.Mouse.ButtonDown += Mouse_ButtonDown;
            sceneManager.Keyboard.KeyDown += Keyboard_KeyDown;

            checkIfPlayer1SetNewHighScore();
        }

        /// <summary>
        /// Overload for the game over scene constructor for single player game. Performs the highscore set check for player 1 
        /// then performs the check on player 2.
        /// </summary>
        public GameOverScene(SceneManager sceneManager, int pPlayerScore, int pPlayerScore2, GameScene.GameType pGameType)
            : this(sceneManager, pPlayerScore, pGameType)
        {
            // 


            player2 = new player();
            player2.score = pPlayerScore2;
            checkIfPlayer2SetNewHighScore();
        }

        #region Scene behaviours

        /// <summary>
        /// Checks if player 1 has set a new high score and update the player member hasSetHighscore.
        /// </summary>
        private void checkIfPlayer1SetNewHighScore()
        {
            // Check if a new high score has been set
            if (player1.score > sceneManager.networkManager.highscoreScores[0])
            {
                player1.hasSetHighScore = true;
            }
            else
            {
                player1.hasSetHighScore = false;
            }
        }
        private void checkIfPlayer2SetNewHighScore()
        {
            // Check if a new high score has been set
            if (player2.score > sceneManager.networkManager.highscoreScores[0])
            {
                player2.hasSetHighScore = true;
            }
            else
            {
                player2.hasSetHighScore = false;
            }
        }

        /// <summary>
        /// Method called when the player has inputted their name and pressed enter to set the position in the array.
        /// Sends the updated high score table to the server to update.
        /// </summary>
        private void setNewHighscore(player pPlayer)
        {
            float width = sceneManager.Width, height = sceneManager.Height, fontSize = Math.Min(width, height) / 20f;

            // Find the index of the next larger element to the player score.
            pPlayer.highscorePosition = Array.BinarySearch(sceneManager.networkManager.highscoreScores, pPlayer.score);

            // If the score is higher than top highscore binary search returns negative number.
            if (pPlayer.highscorePosition < 0)
            {
                // Specifically returns the bitwise complement of the index of the last element plus 1.
                pPlayer.highscorePosition = Math.Abs(pPlayer.highscorePosition) - 1; // This reassigns the index to top of high score table.
            }

            // Moves all the high score entries below player's score down an index.
            for (int i = 1; i < pPlayer.highscorePosition; i++)
            {
                sceneManager.networkManager.highscoreScores[i - 1] = sceneManager.networkManager.highscoreScores[i];
                sceneManager.networkManager.highscoreNames[i - 1] = sceneManager.networkManager.highscoreNames[i];
            }

            // Add the player score to the high score arrays.
            sceneManager.networkManager.highscoreScores[pPlayer.highscorePosition - 1] = pPlayer.score;
            sceneManager.networkManager.highscoreNames[pPlayer.highscorePosition - 1] = pPlayer.name;

            // Add a correct message send to server to update the high scores table.

            string highscore = null;
            for (int i = 0; i < sceneManager.networkManager.highscoreNames.Length; i++)
            {
                highscore += sceneManager.networkManager.highscoreNames[i];
                highscore += '@';
                highscore += sceneManager.networkManager.highscoreScores[i];

                if (i != sceneManager.networkManager.highscoreNames.Length - 1)
                    highscore += '@';
            }

            // Updates the highscore table on the server every time a highscore is set.
            sceneManager.networkManager.UpdateServerHighscoreTable(highscore);
        }


        #endregion

        public void Update(FrameEventArgs e)
        {
        }
        public void Render(FrameEventArgs e)
        {
            GL.Viewport(0, 0, sceneManager.Width, sceneManager.Height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, sceneManager.Width, 0, sceneManager.Height, -1, 1);

            GUI.clearColour = Color.Black;

            //Display the Title
            float width = sceneManager.Width, height = sceneManager.Height, fontSize = Math.Min(width, height) / 20f;

            GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 1, (int)width, (int)(fontSize * 2f)),
                "Game over", (int)fontSize, StringAlignment.Center);

            if (gametype == GameScene.GameType.SinglePlayer)
            {
                GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 7, (int)width, (int)(fontSize)),
                    "Score: " + player1.score, (int)fontSize, StringAlignment.Center);

                if (player1.hasSetHighScore)
                {
                    // Inform the player a new high score has been attained 
                    GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 14, (int)width, (int)(fontSize)),
                    "Player 1 New highscore set!", (int)fontSize, StringAlignment.Center);

                    // If the player has not set their high score yet, show prompt on screen for player to 
                    // input their name and set their highscore once they hit enter.

                    GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 18, (int)width, (int)(fontSize)),
                    "Please enter your name and press enter:", (int)fontSize, StringAlignment.Center);

                    GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 21, (int)width, (int)(fontSize)),
                    player1.name, (int)fontSize, StringAlignment.Center);
                }
            }

            else if (gametype == GameScene.GameType.Multiplayer)
            {

                if (player1.hasSetHighScore && !player1.HasHighScoreAdded) // renders while player 1 is setting their name, passes to player 2 when they hit enter in onKeyPress method.
                {
                    GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 7, (int)width, (int)(fontSize)),
                    "Score: " + player1.score, (int)fontSize, StringAlignment.Center);

                    GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 14, (int)width, (int)(fontSize)),
                    "Player 1 New highscore set!", (int)fontSize, StringAlignment.Center);

                    GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 18, (int)width, (int)(fontSize)),
                    "Please enter your name and press enter:", (int)fontSize, StringAlignment.Center);

                    GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 21, (int)width, (int)(fontSize)),
                    player1.name, (int)fontSize, StringAlignment.Center);
                }
                else if (player2.hasSetHighScore)
                {
                    GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 7, (int)width, (int)(fontSize)),
                    "Score: " + player2.score, (int)fontSize, StringAlignment.Center);

                    GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 14, (int)width, (int)(fontSize)),
                    "Player 2 New highscore set!", (int)fontSize, StringAlignment.Center);

                    GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 18, (int)width, (int)(fontSize)),
                    "Please enter your name and press enter:", (int)fontSize, StringAlignment.Center);

                    GUI.Label(new Rectangle(0, (int)(fontSize / 2f) * 21, (int)width, (int)(fontSize)),
                    player2.name, (int)fontSize, StringAlignment.Center);
                }
            }

            GUI.Render();
        }

        #region Input
        private void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    break;
                case MouseButton.Right:
                    if (gametype == GameScene.GameType.SinglePlayer)
                    {
                        if (!player1.hasSetHighScore) // allows player to quit screen using right click if they did not set the highscore
                        {
                            sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                            sceneManager.Keyboard.KeyDown -= Keyboard_KeyDown;
                            sceneManager.StartMenu();
                        }
                    }
                    else if (gametype == GameScene.GameType.Multiplayer)
                    {
                        if (!player1.hasSetHighScore && !player2.hasSetHighScore)
                        {
                            sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                            sceneManager.Keyboard.KeyDown -= Keyboard_KeyDown;
                            sceneManager.StartMenu();
                        }
                    }


                    break;
            }
        }

        public void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (gametype == GameScene.GameType.SinglePlayer)
            {
                // If a new high score has been attained and the score has not been added to the highscore table yet read in the name.
                if (player1.hasSetHighScore)
                {
                    if (e.Key == Key.Enter)
                    {
                        setNewHighscore(player1);

                        // EXIT SCENE
                        sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                        sceneManager.Keyboard.KeyDown -= Keyboard_KeyDown;
                        sceneManager.StartHighscoreScene();
                    }
                    else if (e.Key == Key.BackSpace)
                    {
                        player1.name = player1.name.Remove(player1.name.Length - 1);
                    }
                    else
                    {
                        player1.name += e.Key.ToString();
                    }
                }
            }


            else if (gametype == GameScene.GameType.Multiplayer)
            {
                if (player1.hasSetHighScore && !player1.HasHighScoreAdded) // if player 1 set a highscore and hasnt added it yet
                {
                    if (e.Key == Key.Enter)
                    {
                        setNewHighscore(player1);
                        player1.HasHighScoreAdded = true;
                        checkIfPlayer2SetNewHighScore(); // check if player 2 set a high score after setting player 1 highscore.

                        if (!player2.hasSetHighScore)
                        {
                            // EXIT SCENE
                            sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                            sceneManager.Keyboard.KeyDown -= Keyboard_KeyDown;
                            sceneManager.StartHighscoreScene();
                        }                    
                    }
                    else if (e.Key == Key.BackSpace)
                    {
                        player1.name = player1.name.Remove(player1.name.Length - 1);
                    }
                    else
                    {
                        player1.name += e.Key.ToString();
                    }
                }
                else if (player2.hasSetHighScore)
                {
                    if (e.Key == Key.Enter)
                    {
                        setNewHighscore(player2);
                        player2.HasHighScoreAdded = true;

                        // EXIT SCENE
                        sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                        sceneManager.Keyboard.KeyDown -= Keyboard_KeyDown;
                        sceneManager.StartHighscoreScene();
                    }
                    else if (e.Key == Key.BackSpace)
                    {
                        player2.name = player2.name.Remove(player2.name.Length - 1);
                    }
                    else
                    {
                        player2.name += e.Key.ToString();
                    }
                }


            }
        }
        #endregion
    }
}
