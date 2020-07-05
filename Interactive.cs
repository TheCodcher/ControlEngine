using System;
using System.Collections.Generic;
using System.Text;
using ControlEngine.Extended;
using System.Drawing;
using System.Windows.Forms;
using ControlEngine.Graphic;
using ControlEngine.Collisions;

namespace ControlEngine
{
    namespace Interactive
    {
        interface ICanBeInteractive
        {
            public event Action<EPoint,MouseButtons> UserClick;
        }
        abstract class InteractiveObject 
        {
            private ICanBeInteractive BaseInteractive;
            public event Action<MouseButtons> BeClicked;
            public List<MouseButtons> Subscriptions = new List<MouseButtons>();
            public InteractiveObject(ICanBeInteractive BaseInteractive)
            {
                this.BaseInteractive = BaseInteractive;
                BaseInteractive.UserClick += ClickHandler;
            }
            public ICanBeInteractive GetBaseInteractive() => BaseInteractive;
            private void ClickHandler(EPoint Place, MouseButtons PressedKey)
            {
                if (Subscriptions.Count != 0 && !Subscriptions.Contains(PressedKey)) return;
                var gArea = GetBoard();
                if (!Place.Include(gArea)) return;
                var shellPoints = GetBoarderPoints();
                if (shellPoints == null)
                {
                    Active(PressedKey);
                    BeClicked?.Invoke(PressedKey);
                    return;
                }
                var Areas = new[]
                {
                    new Rectangle(gArea.Location, Place - gArea.Location),
                    new Rectangle(Place.X,gArea.Location.Y, gArea.Size.Width - Place.X, Place.Y - gArea.Location.Y),
                    new Rectangle(gArea.Location.X, Place.Y, Place.X - gArea.Location.X, gArea.Size.Height - Place.Y),
                    new Rectangle(Place, (EPoint)gArea.Size - Place)
                };
                var flags = new bool[4];
                foreach(var p in shellPoints)
                {
                    for(int i = 0; i < 4; i++)
                    {
                        if (!flags[i]) //в верхней include, а в других посос
                        {
                            flags[i] = p.Include(Areas[i]);
                            if (flags[i]) break;
                        }
                    }
                    if (flags[0] && flags[1] && flags[2] && flags[3])
                    {
                        Active(PressedKey);
                        BeClicked?.Invoke(PressedKey);
                        return;
                    }
                }
            }
            protected abstract void Active(MouseButtons PressedKey);
            protected abstract Rectangle GetBoard();
            protected virtual EPoint[] GetBoarderPoints()
            {
                return null;
            }
        }

    }
}
