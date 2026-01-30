
namespace HoverTanks.Entities
{
    public partial class Pawn
    {
        public class Configuration
        {
            private readonly Pawn _pawn;

            internal Configuration(Pawn pawn)
            {
                _pawn = pawn;
            }

            public Configuration AdjustForwardSpeed(float amount)
            {
                _pawn._forwardSpeed.Base += amount;
                return this;
            }

            public Configuration AddForwardSpeedFactor(float factor)
            {
                _pawn._forwardSpeed.AddFactor(factor);
                return this;
            }

            public Configuration RemoveForwardSpeedFactor(float factor)
            {
                _pawn._forwardSpeed.RemoveFactor(factor);
                return this;
            }

            public Configuration AdjustStrafeSpeed(float amount)
            {
                _pawn._strafeSpeed.Base += amount;
                return this;
            }

            public Configuration AddStrafeSpeedFactor(float factor)
            {
                _pawn._strafeSpeed.AddFactor(factor);
                return this;
            }

            public Configuration RemoveStrafeSpeedFactor(float factor)
            {
                _pawn._strafeSpeed.RemoveFactor(factor);
                return this;
            }

            public Configuration AdjustReverseSpeed(float amount)
            {
                _pawn._reverseSpeed.Base += amount;
                return this;
            }

            public Configuration AddReverseSpeedFactor(float factor)
            {
                _pawn._reverseSpeed.AddFactor(factor);
                return this;
            }

            public Configuration RemoveReverseSpeedFactor(float factor)
            {
                _pawn._reverseSpeed.RemoveFactor(factor);
                return this;
            }

            public Configuration AddTiltVectorAccelFactor(float factor)
            {
                _pawn._tiltVectorAccel.AddFactor(factor);
                return this;
            }

            public Configuration RemoveTiltVectorAccelFactor(float factor)
            {
                _pawn._tiltVectorAccel.RemoveFactor(factor);
                return this;
            }

            public Configuration AddTurnSpeedFactor(float factor)
            {
                _pawn._turnSpeed.AddFactor(factor);
                return this;
            }

            public Configuration RemoveTurnSpeedFactor(float factor)
            {
                _pawn._turnSpeed.RemoveFactor(factor);
                return this;
            }
        }
    }
}
