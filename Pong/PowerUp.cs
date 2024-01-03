using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zombie2
{
    class PowerUp
    {
        public bool active;
        public int duration;
        public int startTime;
        public String name;
        public PowerUp(int duration, string name)
        {
            this.duration = duration;
            this.name = name;
        }
        public void Initiate(int startTime)
        {
            if (!active)
            {
                this.startTime = startTime;
                active = true;
            }
        }
        public bool Update(int currentTime)
        {
            if (active) { active = (currentTime <= duration + startTime); }
            return active;
        }
    }

}
