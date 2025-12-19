using System.Numerics;

namespace KemiK_0_3.Sim
{
    internal class Collision(Compound compound1, Compound compound2, Vector2 I)
    {
        public Compound Compound1 { get; set; } = compound1;
        public Compound Compound2 { get; set; } = compound2;
        public Vector2 I { get; } = I;
    }


}
