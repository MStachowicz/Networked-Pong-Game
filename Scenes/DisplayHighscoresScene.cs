using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;

namespace PongGame
{
    class DisplayHighscoresScene : Scene, IScene
    {
        public DisplayHighscoresScene(SceneManager sceneManager) : base(sceneManager)
        {
            // Set the title of the window
            sceneManager.Title = "Pong - highscores";

            // Set the Render and Update delegates to the Update and Render methods of this class
            sceneManager.renderer = Render;
            sceneManager.updater = Update;

            sceneManager.Mouse.ButtonDown += Mouse_ButtonDown;
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

            GUI.Label(new Rectangle(0, (int)(fontSize / 2f), (int)width, (int)(fontSize * 2f)),
                "Highscores", (int)fontSize, StringAlignment.Center);


            int y = ((int)(height / sceneManager.networkManager.highscoreScores.Length) / 5);
            int yOffset = (int)(fontSize / 2f) * 4;

            // show the high scores on screen
            for (int i = 1; i < sceneManager.networkManager.highscoreScores.Length + 1; i++)
            {
                GUI.Label(
                    new Rectangle(-20, yOffset + y * i,   (int)width,     (int)(fontSize) * 2),
                    i.ToString() + ".    " + sceneManager.networkManager.highscoreNames[sceneManager.networkManager.highscoreNames.Length - i], 
                    (int)fontSize / 2, 
                    StringAlignment.Center);


                GUI.Label(
                    new Rectangle((int)fontSize * 3, yOffset + y * i,   (int)width,     (int)(fontSize) * 2),
                    sceneManager.networkManager.highscoreScores[sceneManager.networkManager.highscoreScores.Length - i].ToString(),
                    (int)fontSize / 2,
                    StringAlignment.Center);
            }


            GUI.Render();
        }
        public void Update(FrameEventArgs e)
        {
        }

        private void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    break;
                case MouseButton.Right:
                    sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                    sceneManager.StartMenu();
                    break;
            }
        }
    }
}
