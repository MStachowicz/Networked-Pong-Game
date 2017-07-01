using System;
using OpenTK;

namespace PongGame
{
    class AIPaddle : Paddle
    {
        public enum difficulty { easy, medium, hard, impossible };
        private static int difficultyModifier = 18;

        public static void ChangeDifficulty(difficulty pDifficultySelected)
        {
            switch (pDifficultySelected)
            {
                case difficulty.easy:
                    difficultyModifier = 25;
                    break;
                case difficulty.medium:
                    difficultyModifier = 18;
                    break;
                case difficulty.hard:
                    difficultyModifier = 10;
                    break;
                case difficulty.impossible:
                    difficultyModifier = 1;
                    break;
            }
        }

        public AIPaddle(int x, int y) : base(x, y)
        {  
        }

        public override void Update(float dt)
        {
            position += velocity;
        }

        public void Move(Vector2 ballPosition)
        {
            velocity.Y = ((ballPosition.Y - position.Y) * (1.0f / difficultyModifier));
        }
    }
}
