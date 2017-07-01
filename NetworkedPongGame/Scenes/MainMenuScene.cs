using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using System.Drawing.Imaging;


namespace PongGame
{
    class MainMenuScene : Scene, IScene
    {
        Rectangle HighscorePosition;
        Rectangle SinglePlayerPosition;
        Rectangle MultiplayerPosition;
        Rectangle NetworkedMultiplayerPosition;
        Rectangle SelectAIDifficultyPosition;


        Color highScoreColor = Color.White;
        Color SinglePlayerColor = Color.White;
        Color MultiplayerColor = Color.White;
        Color NetworkedMultiplayerColor = Color.White;
        Color SelectAIDifficultyColor = Color.White;


        public MainMenuScene(SceneManager sceneManager) : base(sceneManager)
        {
            // Set the title of the window
            sceneManager.Title = "Pong - Main Menu";
            // Set the Render and Update delegates to the Update and Render methods of this class
            sceneManager.renderer = Render;
            sceneManager.updater = Update;
            // Set Keyboard events to go to a method in this class
            sceneManager.Mouse.ButtonDown += Mouse_ButtonDown;
            sceneManager.Mouse.Move += MouseHover;
        }

        private void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    if (HighscorePosition.Contains(e.X, e.Y)) // If the highscore option is clicked on.
                    {
                        sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                        sceneManager.Mouse.Move -= MouseHover;
                        sceneManager.StartHighscoreScene();
                        break;
                    }
                    else if (SinglePlayerPosition.Contains(e.X, e.Y)) // If the highscore option is clicked on.
                    {
                        sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                        sceneManager.Mouse.Move -= MouseHover;
                        sceneManager.StartNewGame(GameScene.GameType.SinglePlayer, false);
                        break;
                    }
                    else if (MultiplayerPosition.Contains(e.X, e.Y)) // If the multiplayer option is clicked on.
                    {
                        sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                        sceneManager.Mouse.Move -= MouseHover;
                        sceneManager.StartNewGame(GameScene.GameType.Multiplayer, true);
                        break;
                    }
                    else if (NetworkedMultiplayerPosition.Contains(e.X, e.Y)) // If the network option is clicked on.
                    {
                        sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                        sceneManager.Mouse.Move -= MouseHover;
                        // lobby uses UDP broadcast to find opponent and set up game in gamscene constructor
                        sceneManager.EnterLobby();
                        break;
                    }
                    else if (SelectAIDifficultyPosition.Contains(e.X, e.Y))
                    {
                        sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                        sceneManager.Mouse.Move -= MouseHover;
                        sceneManager.StartAIDifficultySelectScene();
                        break;
                    }
                    break;
                case MouseButton.Right:
                    break;
            }
        }

        /// <summary>
        /// Performs text highlighting for all the main menu options when the mouse position is over them.
        /// </summary>
        public void MouseHover(object sender, MouseMoveEventArgs e)
        {
            // Main menu text highlight for mouse hover over.
            if (HighscorePosition.Contains(e.X, e.Y))
                highScoreColor = Color.Red;
            else
                highScoreColor = Color.White;

            if (SinglePlayerPosition.Contains(e.X, e.Y))
                SinglePlayerColor = Color.Red;
            else
                SinglePlayerColor = Color.White;

            if (MultiplayerPosition.Contains(e.X, e.Y))
                MultiplayerColor = Color.Red;
            else
                MultiplayerColor = Color.White;

            if (NetworkedMultiplayerPosition.Contains(e.X, e.Y))
                NetworkedMultiplayerColor = Color.Red;
            else
                NetworkedMultiplayerColor = Color.White;

            if (SelectAIDifficultyPosition.Contains(e.X, e.Y))
                SelectAIDifficultyColor = Color.Red;
            else
                SelectAIDifficultyColor = Color.White;
        }

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

            SinglePlayerPosition = new Rectangle(0, (int)(fontSize / 2f) * 1, (int)width, (int)(fontSize * 2f));
            GUI.Label(SinglePlayerPosition, "Single player", (int)fontSize, StringAlignment.Center, SinglePlayerColor);

            MultiplayerPosition = new Rectangle(0, (int)(fontSize / 2f) * 5, (int)width, (int)(fontSize * 2f));
            GUI.Label(MultiplayerPosition, "Multiplayer", (int)fontSize, StringAlignment.Center, MultiplayerColor);

            NetworkedMultiplayerPosition = new Rectangle(0, (int)(fontSize / 2f) * 9, (int)width, (int)(fontSize * 2f));
            GUI.Label(NetworkedMultiplayerPosition, "Networked multiplayer (coming soon)", (int)fontSize, StringAlignment.Center, NetworkedMultiplayerColor);

            SelectAIDifficultyPosition = new Rectangle(0, (int)(fontSize / 2f) * 18, (int)width, (int)(fontSize * 2f));
            GUI.Label(SelectAIDifficultyPosition, "Select AI difficulty", (int)fontSize, StringAlignment.Center, SelectAIDifficultyColor);

            HighscorePosition = new Rectangle(0, (int)(fontSize / 2f) * 22, (int)width, (int)(fontSize * 2f));
            GUI.Label(HighscorePosition, "Display highscores", (int)fontSize, StringAlignment.Center, highScoreColor);     

            GUI.Render();
        }
    }
}