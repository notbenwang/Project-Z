using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zombie2
{
    class Character
    {
        public Texture2D texture;
        public Vector2 position;
        public float velocity;
        public Direction direction;
        public int health;
        public float angle;
        public float size;
        public Character(Texture2D texture, Vector2 position, float velocity, Direction direction, int health, float angle = 0, float size = 25)
        {
            this.texture = texture;
            this.position = position;
            this.velocity = velocity;
            this.direction = direction;
            this.health = health;
            this.angle = angle;
            this.size = size;
        }
        //public float SetAngle(float angle)
        //{
        //    if (Math.Abs(angle - this.angle) > 0)
        //    {

        //    }
        //}

        public float left() { return position.X - (25 / 2); }
        public float right() { return position.X + (25 / 2); }
        public float top() { return position.Y - (25 / 2); }
        public float bottom() { return position.Y + (25 / 2); }
        public bool Touches(Character other)
        {
            // FROM GAMEBOX
            float l = other.left() - right();
            float r = left() - other.right();
            float t = other.top() - bottom();
            float b = top() - other.bottom();
            return Math.Max(Math.Max(l, r), Math.Max(t, b)) <= 0;
        }
        public float[] Overlap(Character other)
        {
            // FROM GAMEBOX
            float l = other.left() - right();
            float r = left() - other.right();
            float t = other.top() - bottom();
            float b = top() - other.bottom();
            float m = Math.Max(Math.Max(l, r), Math.Max(t, b));
            if (m >= 0) { return new float[] { 0, 0 }; }
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
                position.X = o[0] / 2;
                position.Y = o[1] / 2 + 25;
                other.position.X = -1 * o[0] / 2 - 25;
                other.position.Y = -1 * o[1] / 2 - 25;

            }
        }
    }
}
