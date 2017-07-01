using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using System.Drawing.Imaging;
using System.Timers;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace PongGame
{
    class LobbyScene : Scene, IScene
    {
        bool FoundGame = false;

        System.Timers.Timer GUItimer = new System.Timers.Timer(interval: 1000);
        string dot = ".";
        int dotCount = 0;

        private void tick(object sender, ElapsedEventArgs e)
        {
            dot += ".";
            dotCount++;

            if (dotCount == 3)
            {
                dotCount = 0;
                dot = ".";
            }
        }

        public LobbyScene(SceneManager sceneManager) : base(sceneManager)
        {
            // Set the title of the window
            sceneManager.Title = "Pong - Lobby";
            // Set the Render and Update delegates to the Update and Render methods of this class
            sceneManager.renderer = Render;
            sceneManager.updater = Update;


            // Set Keyboard events to go to a method in this class
            //sceneManager.Mouse.ButtonDown += Mouse_ButtonDown;

            GUItimer.Elapsed += tick;
            GUItimer.Start();


            //int before = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
            //Console.WriteLine(before);

            // Both master and slave peer broadcast their IP.
            string messageToBroadcast = sceneManager.networkManager.MyIP.ToString();

            Thread UDPBroadcastThread = new Thread(() =>
            sceneManager.networkManager.startUDPBroadcast(messageToBroadcast));
            UDPBroadcastThread.Start();

            Thread UDPListenThread = new Thread(() =>
            sceneManager.networkManager.listenforUDPBroadcasts(true));
            UDPListenThread.Start();

            


            //Thread.Sleep(100);
            //int after = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
            //Console.WriteLine("\n\nthreads: " + (after - before) + "\n\n");
            //Console.WriteLine(after);
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


            GUI.Label(
                new Rectangle(0, (int)(fontSize / 2f) * 1, (int)width, (int)(fontSize * 2f)),
                "Lobby", (int)fontSize, StringAlignment.Center);


            if (!FoundGame)
            {
                GUI.Label(
                    new Rectangle(0, (int)(fontSize / 2f) * 5, (int)width, (int)(fontSize * 2f)),
                        "Searching for a game" + dot, (int)fontSize, StringAlignment.Center);
            }
            else
            {
                GUI.Label(
                    new Rectangle(0, (int)(fontSize / 2f) * 5, (int)width, (int)(fontSize * 2f)),
                    "Found game!", (int)fontSize, StringAlignment.Center);
            }

            GUI.Render();
        }

        public void Update(FrameEventArgs e)
        {
        }

        //private void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    switch (e.Button)
        //    {
        //        case MouseButton.Left:
        //            break;
        //        case MouseButton.Right:
        //            break;
        //    }
        //}


    }
}
