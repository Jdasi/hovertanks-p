
namespace HoverTanks.Entities
{
    public struct AnimData
    {
        public float ForwardDot;
        public float RightDot;
        public float TurnSpeed;
        public bool IsTurboOn;

        public AnimData(float forwardDot, float rightDot, float turnSpeed, bool isTurboOn)
        {
            ForwardDot = forwardDot;
            RightDot = rightDot;
            TurnSpeed = turnSpeed;
            IsTurboOn = isTurboOn;
        }
    }
}
