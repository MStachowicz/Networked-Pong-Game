using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Timers;
using System.Threading;
using System.Net.Sockets;

namespace PongGame
{
    class SceneManager : GameWindow
    {
        #region Members
        Scene scene;
        static int width = 0;
        static int height = 0;

        public delegate void SceneDelegate(FrameEventArgs e);
        public SceneDelegate renderer;
        public SceneDelegate updater;

        public NetworkManager networkManager;

        #endregion

        /// <summary>
        /// Constructor for the SceneManager class.
        /// </summary>
        public SceneManager()
        {
            Mouse.ButtonDown += Mouse_ButtonDown;
        }

        #region Getters + Setters
        public static int WindowWidth
        {
            get { return width; }
        }

        public static int WindowHeight
        {
            get { return height; }
        }
        #endregion

        protected override void OnLoad(EventArgs e)
        {
            networkManager = new NetworkManager(this);
            // Sends a message to the server to set up the highscore table.
            networkManager.UpdateLocalHighScore();

            base.OnLoad(e);

            GL.Enable(EnableCap.DepthTest);

            base.Width = 1300;
            base.Height = 512;
            SceneManager.width = Width;
            SceneManager.height = Height;

            //Load the GUI
            GUI.SetUpGUI(Width, Height);

            StartMenu();
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            updater(e);
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            renderer(e);

            GL.Flush();
            SwapBuffers();
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
            SceneManager.width = Width;
            SceneManager.height = Height;

            //Load the GUI
            GUI.SetUpGUI(Width, Height);
        }

        private void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    //StartNewGame();
                    break;
                case MouseButton.Right:
                    //StartMenu();
                    break;
            }
        }


        #region Scenes
        public void StartNewGame(GameScene.GameType pGameType, bool pMasterPeer)
        {
            scene = new GameScene(this, pGameType, pMasterPeer);
        }

        public void StartMenu()
        {
            scene = new MainMenuScene(this);
        }

        /// <summary>
        /// Starts the game over scene for a single player game.
        /// </summary>
        /// <param name="pPlayerScore"></param>
        public void GameOverMenu(int pPlayerScore)
        {
            scene = new GameOverScene(this, pPlayerScore, GameScene.GameType.SinglePlayer);
        }
        /// <summary>
        /// Starts a game over scene for a multiplayer game.
        /// </summary>
        /// <param name="pPlayerScore"></param>
        /// <param name="pPlayerScore2"></param>
        public void MultiplayerGameOverMenu(int pPlayerScore, int pPlayerScore2)
        {
            scene = new GameOverScene(this, pPlayerScore, pPlayerScore2, GameScene.GameType.Multiplayer);
        }

        public void StartHighscoreScene()
        {
            scene = new DisplayHighscoresScene(this);
        }

        public void StartAIDifficultySelectScene()
        {
            scene = new SelectAIDifficultyScene(this);
        }

        public void EnterLobby()
        {
            scene = new LobbyScene(this); 
        }

        #endregion
    }

}

