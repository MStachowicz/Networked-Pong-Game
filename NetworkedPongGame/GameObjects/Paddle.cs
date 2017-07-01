using System;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace PongGame
{
    abstract class Paddle : GameObject
    {
        public Paddle()
        {
            position.X = 0;
            position.Y = 0;
        }

        public Paddle(int x, int y)
        {
            position.X = x;
            position.Y = y;
        }

        public Vector3 mScale = new Vector3(1.0f, 1.0f, 0.0f);

        public enum PaddleSide
        {
            left, right
        };

        // STICKY POWERUP
        public bool sticky;
        public Ball ballStuckToPaddle;

        /// <summary>
        /// Checks if the paddle has collided with the paramater ball.
        /// </summary>
        /// <param name="pBall"></param>
        /// <returns></returns>
        public bool hasCollidedWithBall(Ball pBall, PaddleSide pPaddleSide)
        {
                switch (pPaddleSide)
                {
                    case PaddleSide.right:
                        if ((Position.X - pBall.Position.X) < pBall.Radius &&               // check ball x matches paddle
                            (pBall.Position.Y > ((Position.Y - (35 * mScale.Y)))) &&         // check the ball y is above paddle bottom
                            (pBall.Position.Y < ((Position.Y + (35 * mScale.Y)))))           // check ball y is below paddle top                                            
                        {
                        if (sticky && !pBall.stuck && pBall.ballType == Ball.BallType.standard)
                        {
                            pBall.stuck = true;
                            ballStuckToPaddle = pBall;
                            return false;
                        }
                            return true;
                        }
                        else
                        {
                            return false;
                        }

                    case PaddleSide.left:
                        if ((pBall.Position.X - Position.X) < pBall.Radius &&
                            (pBall.Position.Y > ((Position.Y - (35.0f * mScale.Y)))) &&
                            (pBall.Position.Y < ((Position.Y + (35.0f * mScale.Y)))))
                        {
                            if (sticky && !pBall.stuck && pBall.ballType == Ball.BallType.standard)
                            {
                                pBall.stuck = true;
                                ballStuckToPaddle = pBall;
                                return false;
                            }
                            return true;                          
                        }
                        else
                        {
                            return false;
                        }
                }
            
            return false;
        }

        public override void Render(Matrix4 projectionMatrix)
        {
            GL.UseProgram(pgmID);
            GL.BindVertexArray(vao_Handle);

            Matrix4 worldMatrix = Matrix4.CreateScale(mScale) * Matrix4.CreateTranslation(position.X, position.Y, 0);
            Matrix4 worldViewProjection = worldMatrix * viewMatrix * projectionMatrix;
            GL.UniformMatrix4(uniform_mview, false, ref worldViewProjection);

            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        public void Init()
        {
            // Create and load shader program
            pgmID = GL.CreateProgram();
            LoadShader("Shaders/vs.glsl", ShaderType.VertexShader, pgmID, out vsID);
            LoadShader("Shaders/fs.glsl", ShaderType.FragmentShader, pgmID, out fsID);
            GL.LinkProgram(pgmID);
            Console.WriteLine(GL.GetProgramInfoLog(pgmID));

            attribute_vpos = GL.GetAttribLocation(pgmID, "a_Position");
            attribute_vcol = GL.GetAttribLocation(pgmID, "a_Colour");
            uniform_mview = GL.GetUniformLocation(pgmID, "WorldViewProj");

            if (attribute_vpos == -1 || attribute_vcol == -1 || uniform_mview == -1)
            {
                Console.WriteLine("Error binding attributes");
            }

            // Store geometry in vertex buffer 
            GL.GenVertexArrays(1, out vao_Handle);
            GL.BindVertexArray(vao_Handle);

            GL.GenBuffers(1, out vbo_position);
            GL.GenBuffers(1, out vbo_color);

            vertdata = new Vector3[] {
                new Vector3(-10f, +30f, 0f),
                new Vector3(-10f, -30f, 0f),
                new Vector3(+10f, -30f, 0f),
                new Vector3(+10f, +30f, 0f) };

            coldata = new Vector3[] {
                new Vector3(colour.X, colour.Y, colour.Z),
                new Vector3(colour.X, colour.Y, colour.Z),
                new Vector3(colour.X, colour.Y, colour.Z),
                new Vector3(colour.X, colour.Y, colour.Z) };

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_position);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(vertdata.Length * Vector3.SizeInBytes), vertdata, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_color);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(coldata.Length * Vector3.SizeInBytes), coldata, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, 0, 0);

            GL.BindVertexArray(0);

            viewMatrix = Matrix4.Identity;
        }

        private void LoadShader(String filename, ShaderType type, int program, out int address)
        {
            address = GL.CreateShader(type);
            using (StreamReader sr = new StreamReader(filename))
            {
                GL.ShaderSource(address, sr.ReadToEnd());
            }
            GL.CompileShader(address);
            GL.AttachShader(program, address);
            Console.WriteLine(GL.GetShaderInfoLog(address));
        }
    }
}
