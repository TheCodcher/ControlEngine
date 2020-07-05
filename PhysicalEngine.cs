using System;
using System.Collections.Generic;
using System.Text;
using ControlEngine;
using ControlEngine.Collisions;
using ControlEngine.Extended;
using ControlEngine.Graphic;

namespace ControlEngine
{
    namespace PhysicalEngine
    {
        abstract class PhysicalObject : MaterialObject
        {
            //понятия не имею что вообще может пригодиться
            public double Mass { get; set; } = 1;
            public double FrictionForce { get; set; } = 0;
            public Vector Gravity { get; set; } = Vector.Empty;
            public double Speed { get; set; } = 1;

            public Vector GetDirectionResult()
            {
                return (Direction * Speed + (Gravity * Mass)) - FrictionForce * Direction;
            }
            public double GetImpulse() => Speed * Mass;
        }
    }
}
