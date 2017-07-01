using System;

namespace PongGame
{
    class PlayerPaddle : Paddle
    {
        public PlayerPaddle(int x, int y) : base(x, y)
        {
        }

        public override void Update(float dt)
        {
            if (moveUp)
            {
                Move(paddleSpeed * dt);
            }
            if (moveDown)
            {
                Move(-paddleSpeed * dt);
            }
        }
        public float paddleSpeed = 200;
        public bool moveUp;
        public bool moveDown;

        public void Move(float dy)
        {
            position.Y += dy;

            if (position.Y < 0)
                position.Y = 0;
            else if (position.Y > SceneManager.WindowHeight)
                position.Y = SceneManager.WindowHeight;

            // Moves the ball stuck to the paddle.
            if (sticky == true)
            {
                if (ballStuckToPaddle != null)
                {
                    ballStuckToPaddle.Position = new OpenTK.Vector2(
                            ballStuckToPaddle.Position.X,
                            ballStuckToPaddle.Position.Y + dy);
                }
            }
        }
    }
}
