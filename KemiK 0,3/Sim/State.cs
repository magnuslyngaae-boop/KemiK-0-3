namespace KemiK_0_3.Sim
{
    class State
    {
        private const double k_B = 1.380649e-23; // J/K
        private const double N_A = 6.02214076e23; // 1/mol
        private const double V = 1.3824E-18; // L
        public int Time { get; }
        public Compound[] Compounds { get; }
        public float Temperatur { get; }
        public int[] CompundFordeling { get; set; } = [0, 0, 0, 0, 0]; // A, B, C, D, X
        public double[] Konc { get; set; } = [0, 0, 0, 0, 0]; // A, B, C, D, X
        public double EkinGNS { get; }
        public double AproximeretLigevægtskonstant { get; }
        public State(int time, Compound[] compounds)
        {
            Time = time;
            Compounds = compounds;
            double E_kin_total = 0;
            foreach (Compound compound in Compounds)
            {
                switch (compound.Type)
                {
                    case Type.A:
                        CompundFordeling[0] += 1;
                        break;
                    case Type.B:
                        CompundFordeling[1] += 1;
                        break;
                    case Type.C:
                        CompundFordeling[2] += 1;
                        break;
                    case Type.D:
                        CompundFordeling[3] += 1;
                        break;
                    case Type.X:
                        CompundFordeling[4] += 1;
                        break;
                } // putter de forskellige stoffer i deres respektive pladser i arrayet
                E_kin_total += compound.Velocity.LengthSquared() * compound.Mass * 1.66053906660e-27;
            }
            Temperatur = (float)(E_kin_total / (2 * compounds.Length * k_B)); // K
            EkinGNS = E_kin_total / (2 * compounds.Length); // J
            for (int i = 0; i < 5; i++)
            {
                Konc[i] = CompundFordeling[i] / (N_A * V); // mol/L
            }

            AproximeretLigevægtskonstant = (Konc[2] * Konc[3]) / (Konc[0] * Konc[1]); // [C][D]/[A][B]
        }
    }
}

