using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;

namespace PongGame
{
    class SelectAIDifficultyScene : Scene, IScene
    {
        Rectangle ImpossiblePosition;
        Rectangle HardPosition;
        Rectangle MediumPosition;
        Rectangle EasyPosition;

        Color ImpossibleColor;
        Color HardColor;
        Color MediumColor;
        Color EasyColor;

        public SelectAIDifficultyScene(SceneManager sceneManager) : base(sceneManager)
        {
            // Set the title of the window
            sceneManager.Title = "Pong - Select AI Difficulty";

            // Set the Render and Update delegates to the Update and Render methods of this class
            sceneManager.renderer = Render;
            sceneManager.updater = Update;

            // Set Keyboard events to go to a method in this class
            sceneManager.Keyboard.KeyDown += Keyboard_KeyDown;
            // Set Keyboard events to go to a method in this class
            sceneManager.Mouse.ButtonDown += Mouse_ButtonDown;
            sceneManager.Mouse.Move += MouseHover;

            GL.ClearColor(Color.Black);
        }

        public void Update(FrameEventArgs e)
        {
        }


        /// <summary>
        /// Performs text highlighting for all the main menu options when the mouse position is over them.
        /// </summary>
        public void MouseHover(object sender, MouseMoveEventArgs e)
        {
            // Main menu text highlight for mouse hover over.
            if (ImpossiblePosition.Contains(e.X, e.Y))
                ImpossibleColor = Color.Red;
            else
                ImpossibleColor = Color.White;

            if (HardPosition.Contains(e.X, e.Y))
                HardColor = Color.Crimson;
            else
                HardColor = Color.White;

            if (MediumPosition.Contains(e.X, e.Y))
                MediumColor = Color.Blue;
            else
                MediumColor = Color.White;

            if (EasyPosition.Contains(e.X, e.Y))
                EasyColor = Color.Green;
            else
                EasyColor = Color.White;
        }



        public void Render(FrameEventArgs e)
        {
            GL.Viewport(0, 0, sceneManager.Width, sceneManager.Height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, sceneManager.Width, 0, sceneManager.Height, -1, 1);

            GUI.clearColour = Color.Black;

            float width = sceneManager.Width, height = sceneManager.Height, fontSize = Math.Min(width, height) / 20f;

            Rectangle TitlePosition = new Rectangle(0, (int)(fontSize / 2f) * 1, (int)width, (int)(fontSize * 3f));
            GUI.Label(TitlePosition, "Select a difficulty", (int)fontSize, StringAlignment.Center);

            ImpossiblePosition = new Rectangle(0, (int)(fontSize / 2f) * 7, (int)width, (int)(fontSize * 2f));
            GUI.Label(ImpossiblePosition, "Impossible", (int)fontSize, StringAlignment.Center, ImpossibleColor);

            HardPosition = new Rectangle(0, (int)(fontSize / 2f) * 11, (int)width, (int)(fontSize * 2f));
            GUI.Label(HardPosition, "Hard", (int)fontSize, StringAlignment.Center, HardColor);

            MediumPosition = new Rectangle(0, (int)(fontSize / 2f) * 15, (int)width, (int)(fontSize * 2f));
            GUI.Label(MediumPosition, "Medium", (int)fontSize, StringAlignment.Center, MediumColor);

            EasyPosition = new Rectangle(0, (int)(fontSize / 2f) * 19, (int)width, (int)(fontSize * 2f));
            GUI.Label(EasyPosition, "Easy", (int)fontSize, StringAlignment.Center, EasyColor);


            GUI.Render();
        }

        #region Input
        private void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    if (ImpossiblePosition.Contains(e.X, e.Y))
                    {
                        AIPaddle.ChangeDifficulty(AIPaddle.difficulty.impossible);
                        break;
                    }
                    else if (HardPosition.Contains(e.X, e.Y))
                    {
                        AIPaddle.ChangeDifficulty(AIPaddle.difficulty.hard);
                        break;
                    }
                    if (MediumPosition.Contains(e.X, e.Y))
                    {
                        AIPaddle.ChangeDifficulty(AIPaddle.difficulty.medium);
                        break;
                    }
                    if (EasyPosition.Contains(e.X, e.Y))
                    {
                        AIPaddle.ChangeDifficulty(AIPaddle.difficulty.easy);
                        break;
                    }
                    break;
                case MouseButton.Right:

                    sceneManager.Keyboard.KeyDown -= Keyboard_KeyDown;
                    sceneManager.Mouse.ButtonDown -= Mouse_ButtonDown;
                    sceneManager.Mouse.Move -= MouseHover;

                    sceneManager.StartMenu();
                    break;
            }
        }
        public void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
        }
        #endregion
    }
}
