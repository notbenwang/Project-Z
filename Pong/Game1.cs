using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace Pong
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _backgroundTexture;
        private List<Character> bulletList;
        private List<Character> enemyList;
        private int bulletTimer = 0;
        private int _bulletCD = 35;
        Character player;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }
        enum Direction
        {
            Up,
            Down,
            Left,
            Right,
            UpLeft,
            UpRight,
            DownLeft,
            DownRight
        }
        class Character
        {
            public Texture2D texture;
            public Vector2 position;
            public float velocity;
            public Direction direction;
            public int health;
            public Character(Texture2D texture, Vector2 position, float velocity, Direction direction, int health)
            {
                this.texture = texture;
                this.position = position;
                this.velocity = velocity;
                this.direction = direction;
                this.health = health;
            }
            
            public float left()  {return position.X - (texture.Width / 2);}
            public float right() { return position.X + (texture.Width / 2);}
            public float top() { return position.Y - (texture.Height / 2);}
            public float bottom() { return position.Y + (texture.Height / 2);}
            public bool Touches(Character other)
            {
                // FROM GAMEBOX
                float l = other.left() - right();
                float r = left() - other.right();
                float t = other.top() - bottom();
                float b = top() - other.bottom();
                return Enumerable.Max(new [] {l,b,r,t}) <= 0;
            }
            public float[] Overlap(Character other)
            {
                // FROM GAMEBOX
                float l = other.left() - right();
                float r = left() - other.right();
                float t = other.top() - bottom();
                float b = top() - other.bottom();
                float m = Enumerable.Max(new[] { l, b, r, t });
                if (m >= 0) { return new float[] {0,0}; } 
                else if (m == l) { return new float[] { l, 0 }; }
                else if (m == r) { return new float[] { -1 * r, 0 }; }
                else if (m == t) { return new float[] { 0, t }; }
                else { return new float[] { 0, -1 * b }; }
            }
            public void moveBothToStopOverlap(Character other)
            {
                float[] o = Overlap(other);
                if (o[0] != 0 && o[1] != 0) // o != [0,0]
                {
                    position.X = o[0]/2;
                    position.Y = o[1]/2;
                    other.position.X = -1 * o[0]/2;
                    other.position.Y = -1 * o[1]/2;
                }
            }
        }
        

        private float GetAngleFromDirection(Direction direction) 
        {
            switch (direction)
            {
                case Direction.Right:
                    return 0f;

                case Direction.Left:
                    return (float) Math.PI;

                case Direction.Down:
                    return (float)Math.PI / 2;

                case Direction.Up:
                    return (float)Math.PI * 3 / 2;

                case Direction.UpLeft:
                    return (float)Math.PI * 5 / 4;

                case Direction.UpRight:
                    return (float)Math.PI * 7 / 4;

                case Direction.DownLeft:
                    return (float)Math.PI * 3 / 4;
                
                case Direction.DownRight:
                    return (float)Math.PI / 4;
            }
            return 0f;
        }
        private void MakeBullet(Vector2 position, Direction direction)
        {
            float bullet_speed = 10f;
            Texture2D whiteRect = new Texture2D(GraphicsDevice, 1, 1);
            whiteRect.SetData(new[] { Color.White });
            Character bullet = new Character(whiteRect, position, bullet_speed, direction, 1);
            bulletList.Add(bullet);
        }
        private void MakeEnemy()
        {
            Random rnd = new Random();
            int side = rnd.Next(1,4);
            int randomX = rnd.Next(_graphics.PreferredBackBufferWidth);
            int randomY = rnd.Next(_graphics.PreferredBackBufferHeight);
            Texture2D redRect = new Texture2D(GraphicsDevice, 1, 1);
            redRect.SetData(new[] { Color.Red });
            Vector2 position = new Vector2(0,0);
            float enemy_speed = 1.0f;
            switch (side)
            {
                case 1:
                    position.X = -100;
                    position.Y = randomY;
                    break;
                case 2:
                    position.X = randomX;
                    position.Y = -100;
                    break;
                case 3:
                    position.X = _graphics.PreferredBackBufferWidth + 100;
                    position.Y = randomY;
                    break;
                case 4:
                    position.X = randomX;
                    position.Y = _graphics.PreferredBackBufferHeight + 100;
                    break;
            }
            enemyList.Add(new Character(redRect, position, enemy_speed, Direction.Up, 1));
        }
        private void MoveEnemies(List<Character> enemies)
        {
            enemies.RemoveAll(enemy => enemy.health <= 0);
            foreach (Character enemy in enemies)
            {
                float distY = player.position.Y - enemy.position.Y;
                float distX = player.position.X - enemy.position.X;
                float theta;
                
                if (distY > -5 && distY < 5)
                {
                    if (player.position.X < enemy.position.X) theta = (float)Math.PI * 3;
                    else theta = (float)Math.PI * 2;
                }

                else if (distX > -5 && distX < 5)
                {
                    if (player.position.Y < enemy.position.Y) theta = (float)Math.PI * 3 / 2;
                    else theta = (float)(Math.PI * 5 / 2);
                }
                else
                {
                    theta = (float)(Math.Atan(distY / distX) + (2 * Math.PI));   
                    if (player.position.X < enemy.position.X)
                    {
                        if (player.position.Y < enemy.position.Y) theta -= (float)Math.PI; 
                        else theta += (float)Math.PI;
                    }
                }

                float da = enemy.velocity * (float)Math.Cos(theta);
                float db = enemy.velocity * (float)Math.Sin(theta);
                
                enemy.position.X += da;
                enemy.position.Y += db;
                foreach (Character otherEnemy in enemies)
                {
                    if (otherEnemy.Touches(enemy)) { enemy.moveBothToStopOverlap(otherEnemy); } 
                }
            }
        }
        private void MoveBullets(List<Character> bulletList)
        {
            foreach (Character bullet in bulletList)
            {
                // Collision Check
                foreach (Character enemy in enemyList)
                {
                    if ((bullet.position.X < enemy.position.X + bullet.velocity + 12) &&
                        (bullet.position.X > enemy.position.X - bullet.velocity - 12) && 
                        (bullet.position.Y < enemy.position.Y + bullet.velocity + 12) &&
                        (bullet.position.Y > enemy.position.Y - bullet.velocity - 12))
                    {
                        enemy.health -= 1;
                        bullet.health -= 1;
                    }
                }
                // Movement Check
                if (bullet.health > 0)
                {
                    float dx = 0, dy = 0;
                    switch (bullet.direction)
                    {
                        case Direction.Right:
                            dx = bullet.velocity;
                            break;
                        case Direction.Left:
                            dx = -1 * bullet.velocity;
                            break;
                        case Direction.Down:
                            dy = -1 * bullet.velocity;
                            break;
                        case Direction.Up:
                            dy = bullet.velocity;
                            break;
                        case Direction.UpLeft:
                            dx = -1 * bullet.velocity;
                            dy = bullet.velocity;
                            break;
                        case Direction.UpRight:
                            dx = bullet.velocity;
                            dy = bullet.velocity;
                            break;
                        case Direction.DownRight:
                            dy = -1 * bullet.velocity;
                            dx = bullet.velocity;
                            break;
                        case Direction.DownLeft:
                            dy = -1 * bullet.velocity;
                            dx = -1 * bullet.velocity;
                            break;
                    }
                    float x = bullet.position.X + dx;
                    float y = bullet.position.Y - dy;
                    bullet.position = new Vector2(x, y);
                }
            }
            bulletList.RemoveAll(bullet =>
                    (bullet.position.X > _graphics.PreferredBackBufferWidth) ||
                    (bullet.position.X < 0) ||
                    (bullet.position.Y > _graphics.PreferredBackBufferHeight) ||
                    (bullet.position.Y < 0));
            bulletList.RemoveAll(bullet => bullet.health <= 0);
        }
        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1080;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
            bulletList = new List<Character>();
            enemyList = new List<Character>();
            Vector2 position = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
            float speed = 300f;
            player = new Character(null, position, speed, Direction.Left, 5);

            for (int i = 0; i < 10; i++) MakeEnemy();

            base.Initialize();
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            player.texture = Content.Load<Texture2D>("player");
            _backgroundTexture = Content.Load<Texture2D>("background1");
        }
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var kstate = Keyboard.GetState();

            // Event Readers
            if (kstate.IsKeyDown(Keys.A)) player.position.X -= player.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kstate.IsKeyDown(Keys.D)) player.position.X += player.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kstate.IsKeyDown(Keys.W)) player.position.Y -= player.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kstate.IsKeyDown(Keys.S)) player.position.Y += player.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kstate.IsKeyDown(Keys.Left)) player.direction = Direction.Left;
            if (kstate.IsKeyDown(Keys.Right)) player.direction = Direction.Right;
            
            if (kstate.IsKeyDown(Keys.Up))
            {
                if (kstate.IsKeyDown(Keys.Left)) player.direction = Direction.UpLeft;
                else 
                if (kstate.IsKeyDown(Keys.Right)) player.direction = Direction.UpRight;
                else player.direction = Direction.Up;
            }
            if (kstate.IsKeyDown(Keys.Down))
            {
                if (kstate.IsKeyDown(Keys.Left)) player.direction = Direction.DownLeft;
                else 
                if (kstate.IsKeyDown(Keys.Right)) player.direction = Direction.DownRight;
                else player.direction = Direction.Down;
            }

            // Player Bound Managing
            if (player.position.X < 15) player.position.X = 15;
            else if (player.position.X > _graphics.PreferredBackBufferWidth - 15)
            {
                player.position.X = _graphics.PreferredBackBufferWidth - 15;
            }
            if (player.position.Y < 15) player.position.Y = 15;
            else if (player.position.Y > _graphics.PreferredBackBufferHeight -15)
            {
                player.position.Y = _graphics.PreferredBackBufferHeight - 15;
            }

            // Bullet Time Management
            if (bulletTimer == 0)
            {
                MakeBullet(player.position, player.direction);
                bulletTimer = _bulletCD;
            }
            else bulletTimer--;
            MoveBullets(bulletList);

            MoveEnemies(enemyList);
            // DON'T DELETE
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            _spriteBatch.Begin();
            _spriteBatch.Draw(
                _backgroundTexture,
                new Vector2(0,0),
                null,
                Color.White,
                0,
                new Vector2(player.texture.Width / 2, player.texture.Height / 2),
                new Vector2(0.35f, 0.35f),
                SpriteEffects.None,
                0f
                );
            // Player Sprite
            _spriteBatch.Draw(
                player.texture, 
                player.position, 
                null, 
                Color.White, 
                GetAngleFromDirection(player.direction), 
                new Vector2(player.texture.Width / 2, player.texture.Height / 2), 
                new Vector2(0.15f, 0.15f),
                SpriteEffects.None, 
                0f);
            // Bullets
            foreach(Character bullet in bulletList)
            {
                _spriteBatch.Draw(
                    bullet.texture,
                    bullet.position,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    new Vector2(5f, 5f),
                    SpriteEffects.None,
                    0f);
            }
            // Enemies
            foreach(Character enemy in enemyList)
            {
                _spriteBatch.Draw(
                    enemy.texture,
                    enemy.position,
                    null,
                    Color.Red,
                    0f,
                    Vector2.Zero,
                    new Vector2(25f, 25f),
                    SpriteEffects.None,
                    0f);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
