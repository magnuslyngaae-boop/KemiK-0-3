using System.Diagnostics;
using System.Numerics;

namespace KemiK_0_3.Sim
{
    internal class Simulation
    {
        const double k_amuToRadiusFactor = 3; // nm
        private bool _on = false;
        private readonly List<Compound> _compounds = [];
        private readonly Random random = new();
        private readonly int _simSpeed;
        private int _timeMilliseconds = 0;
        private readonly List<Compound>[,] _grid;
        private readonly float _gridWidth = 0;
        private readonly List<State> _states = [];
        private double _e_a = 0d;
        private double _bindingsenergiA = 0d;
        private double _bindingsenergiB = 0d;
        private double _bindingsenergiC = 0d;
        private double _bindingsenergiD = 0d;
        private readonly double _bindingsenergiChangeRate = 1E-21;
        private int _maxSimTimeMilliseconds = 0;
        private State lastEndSnapshot;
        public int _ABToCD = 1;
        public int _CDToAB = 1;
        public Vector2 SimSize { get; }
        public double BindingsenergiA { get { return _bindingsenergiA; } }
        public double BindingsenergiB { get { return _bindingsenergiB; } }
        public double BindingsenergiC { get { return _bindingsenergiC; } }
        public double BindingsenergiD { get { return _bindingsenergiD; } }
        public double Aktiveringsenergi { get { return _e_a; } }
        public int ABToCD { get { return _ABToCD; } }
        public int CDToAB { get { return _CDToAB; } }
        public int SimTime { get { return _states.Count - 1; } }
        public State GetCurrentState
        {
            get
            {
                if (_on) return lastEndSnapshot;
                {
                    Compound[] snapshot = new Compound[_compounds.Count];
                    for (int i = 0; i < _compounds.Count; i++)
                    {
                        snapshot[i] = new Compound(_compounds[i]);
                    }
                    lastEndSnapshot = new State(_timeMilliseconds, snapshot);
                    return lastEndSnapshot;
                }
                ;
            }
        }
        public Simulation(int numberOfCompunds, float weightA, float weightB, float weightC, float weightD, float weightX, Vector2 simSize, int simSpeed)
        {
            SimSize = simSize;
            _simSpeed = simSpeed;

            float totalWeight = weightA + weightB + weightC + weightD + weightX;

            for (int i = 0; i < numberOfCompunds * weightA / totalWeight; i++)
            {
                float v = (float)(random.NextDouble() * 2 * Math.PI);
                _compounds.Add(new Compound(new Vector2(random.Next((int)SimSize.X), random.Next((int)SimSize.Y)), new Vector2(500 * MathF.Cos(v), 500 * MathF.Sin(v)), Type.A));
            }
            for (int i = 0; i < numberOfCompunds * weightB / totalWeight; i++)
            {
                float v = (float)(random.NextDouble() * 2 * Math.PI);
                _compounds.Add(new Compound(new Vector2(random.Next((int)SimSize.X), random.Next((int)SimSize.Y)), new Vector2(500 * MathF.Cos(v), 500 * MathF.Sin(v)), Type.B));
            }
            for (int i = 0; i < numberOfCompunds * weightC / totalWeight; i++)
            {
                float v = (float)(random.NextDouble() * 2 * Math.PI);
                _compounds.Add(new Compound(new Vector2(random.Next((int)SimSize.X), random.Next((int)SimSize.Y)), new Vector2(500 * MathF.Cos(v), 500 * MathF.Sin(v)), Type.C));
            }
            for (int i = 0; i < numberOfCompunds * weightD / totalWeight; i++)
            {
                float v = (float)(random.NextDouble() * 2 * Math.PI);
                _compounds.Add(new Compound(new Vector2(random.Next((int)SimSize.X), random.Next((int)SimSize.Y)), new Vector2(500 * MathF.Cos(v), 500 * MathF.Sin(v)), Type.D));
            }
            for (int i = 0; i < numberOfCompunds * weightX / totalWeight; i++)
            {
                float v = (float)(random.NextDouble() * 2 * Math.PI);
                _compounds.Add(new Compound(new Vector2(random.Next((int)SimSize.X), random.Next((int)SimSize.Y)), new Vector2(500 * MathF.Cos(v), 500 * MathF.Sin(v)), Type.X));
            }

            if ((float)(Math.Cbrt((float)Type.A) * k_amuToRadiusFactor) > _gridWidth) _gridWidth = (float)(Math.Cbrt((float)Type.A) * k_amuToRadiusFactor);
            if ((float)(Math.Cbrt((float)Type.B) * k_amuToRadiusFactor) > _gridWidth) _gridWidth = (float)(Math.Cbrt((float)Type.B) * k_amuToRadiusFactor);
            if ((float)(Math.Cbrt((float)Type.C) * k_amuToRadiusFactor) > _gridWidth) _gridWidth = (float)(Math.Cbrt((float)Type.C) * k_amuToRadiusFactor);
            if ((float)(Math.Cbrt((float)Type.D) * k_amuToRadiusFactor) > _gridWidth) _gridWidth = (float)(Math.Cbrt((float)Type.D) * k_amuToRadiusFactor);
            if ((float)(Math.Cbrt((float)Type.X) * k_amuToRadiusFactor) > _gridWidth) _gridWidth = (float)(Math.Cbrt((float)Type.X) * k_amuToRadiusFactor);

            _gridWidth = MathF.Sqrt(_gridWidth) * 6;

            _grid = new List<Compound>[(int)(SimSize.X / _gridWidth), (int)(SimSize.Y / _gridWidth)];
            for (int x = 0; x < _grid.GetLength(0); x++)
            {
                for (int y = 0; y < _grid.GetLength(1); y++)
                {
                    _grid[x, y] = [];
                }
            }
            // Gem state
            Compound[] snapshot = new Compound[_compounds.Count];

            for (int i = 0; i < _compounds.Count; i++)
            {
                snapshot[i] = new Compound(_compounds[i]);
            }
            lock (_states)
            {
                _states.Add(new State(_timeMilliseconds, snapshot));
            }
            lastEndSnapshot = new State(_timeMilliseconds, snapshot);
        }
        public State GetState(int timeMilliseconds)
        {
            lock (_states)
            {
                if (_states.Count == 0) return _states[0];
                if (timeMilliseconds < _states.Count) return _states[timeMilliseconds];
                return _states[^1];
            }
        }
        public State[] GetStates()
        {
            lock (_states)
            {
                State[] states = new State[_states.Count];
                for (int i = 0; i < _states.Count; i++)
                {
                    states[i] = _states[i];
                }
                return states;
            }
        }
        public void ModifyTemp(bool increase)
        {
            if (_on) return;
            if (increase)
            {
                foreach (Compound compound in _compounds)
                {
                    compound.Velocity *= 1.05f;
                }
            }
            else
            {
                foreach (Compound compound in _compounds)
                {
                    compound.Velocity *= 0.95f;
                }
            }
        }
        public void ModifyBestand(bool increase, Type type)
        {
            if (_on) return;
            if (increase)
            {
                for (int i = 0; i < 250; i++)
                {
                    float p = (float)(random.NextDouble() * 2 * Math.PI);
                    double v = Math.Sqrt(2 * GetCurrentState.EkinGNS / ((float)type * 1.66053906660e-27));
                    _compounds.Add(new Compound(new Vector2(random.Next((int)SimSize.X), random.Next((int)SimSize.Y)), new Vector2((float)v * MathF.Cos(p), (float)v * MathF.Sin(p)), type));
                }
            }
            else
            {
                int count = 250;
                for (int i = 0; i <= _compounds.Count -1; i++)
                {
                    if (_compounds[i].Type == type)
                    {
                        _compounds.Remove(_compounds[i]);
                        count--;
                        i--;
                    }
                    if (count <= 0) { break; }
                }
            }
        }
        public void ModifyBindingsenergiA(bool increase)
        {
            if (_on) return;
            if (increase)
            {
                _bindingsenergiA += _bindingsenergiChangeRate;
            }
            else
            {
                _bindingsenergiA -= _bindingsenergiChangeRate;
                _bindingsenergiA = _bindingsenergiA < 0 ? 0 : _bindingsenergiA;
            }
        }
        public void ModifyBindingsenergiB(bool increase)
        {
            if (_on) return;
            if (increase)
            {
                _bindingsenergiB += _bindingsenergiChangeRate;
            }
            else
            {
                _bindingsenergiB -= _bindingsenergiChangeRate;
                _bindingsenergiB = _bindingsenergiB < 0 ? 0 : _bindingsenergiB;
            }
        }
        public void ModifyBindingsenergiC(bool increase)
        {
            if (_on) return;
            if (increase)
            {
                _bindingsenergiC += _bindingsenergiChangeRate;
            }
            else
            {
                _bindingsenergiC -= _bindingsenergiChangeRate;
                _bindingsenergiC = _bindingsenergiC < 0 ? 0 : _bindingsenergiC;
            }
        }
        public void ModifyBindingsenergiD(bool increase)
        {
            if (_on) return;
            if (increase)
            {
                _bindingsenergiD += _bindingsenergiChangeRate;
            }
            else
            {
                _bindingsenergiD -= _bindingsenergiChangeRate;
                _bindingsenergiD = _bindingsenergiD < 0 ? 0 : _bindingsenergiD;
            }
        }
        public void ModifyAktiveringsenergi(bool increase)
        {
            if (_on) return;
            if (increase)
            {
                _e_a += _bindingsenergiChangeRate;
            }
            else
            {
                _e_a -= _bindingsenergiChangeRate;
                _e_a = _e_a < 0 ? 0 : _e_a;
            }
        }
        public void ModifyABToCD(bool increase)
        {
            if (_on) return;
            if (increase)
            {
                _ABToCD++;
            }
            else
            {
                _ABToCD = _ABToCD - 1 < 0 ? 0 : _ABToCD - 1;
            }
        }
        public void ModifyCDToAB(bool increase)
        {
            if (_on) return;
            if (increase)
            {
                _CDToAB++;
            }
            else
            {
                _CDToAB = _CDToAB - 1 < 0 ? 0 : _CDToAB - 1;
            }
        }
        public void Run()
        {
            if (_on) return;

            _on = true;

            _maxSimTimeMilliseconds += 2500; // kør 5 sekunder mere hver gang

            Task.Run(() =>
            {
                while (_on)
                {
                    // Put i felter
                    foreach (Compound compound in _compounds)
                    {
                        _grid[Math.Clamp((int)(compound.Position.X / _gridWidth), 0, _grid.GetLength(0) - 1), Math.Clamp((int)(compound.Position.Y / _gridWidth), 0, _grid.GetLength(1) - 1)].Add(compound);
                    }

                    #region Movement
                    // Ryk Compounds
                    _timeMilliseconds += _simSpeed;

                    Parallel.ForEach(_compounds, compound =>
                    {
                        compound.Move(_simSpeed);
                    });

                    for (int x = 0; x < _grid.GetLength(0); x++)
                    {
                        foreach (Compound compound in _grid[x, 0])
                        {
                            compound.CheckCollisionTop();
                        }
                    }
                    for (int x = 0; x < _grid.GetLength(0); x++)
                    {
                        foreach (Compound compound in _grid[x, _grid.GetLength(1) - 1])
                        {
                            compound.CheckCollisionBottom(SimSize);
                        }
                    }
                    for (int y = 0; y < _grid.GetLength(1); y++)
                    {
                        foreach (Compound compound in _grid[0, y])
                        {
                            compound.CheckCollisionLeft();
                        }
                    }
                    for (int y = 0; y < _grid.GetLength(1); y++)
                    {
                        foreach (Compound compound in _grid[_grid.GetLength(0) - 1, y])
                        {
                            compound.CheckCollisionRight(SimSize);
                        }
                    }
                    #endregion

                    // registrer kollisioner
                    // sammenlign compounds i nærtliggende felter
                    for (int y = 0; y < _grid.GetLength(1); y++)
                    {
                        for (int x = 0; x < _grid.GetLength(0); x++)
                        {
                            while (_grid[x, y].Count != 0)
                            {
                                Compound compound1 = _grid[x, y][0];

                                for (int xOffset = -1; xOffset < 2; xOffset++)
                                {
                                    for (int yOffset = -1; yOffset < 2; yOffset++)
                                    {
                                        if (x + xOffset >= 0 && x + xOffset < _grid.GetLength(0) && y + yOffset >= 0 && y + yOffset < _grid.GetLength(1))
                                        {
                                            foreach (Compound compound2 in _grid[x + xOffset, y + yOffset])
                                            {
                                                Vector2 I = compound2.Position - compound1.Position;
                                                if (I.Length() < compound1.Radius + compound2.Radius && compound1 != compound2)
                                                {
                                                    BeregnKollision(compound1, compound2, I);
                                                }
                                            }
                                        }
                                    }
                                }
                                _grid[x, y].RemoveAt(0);
                            }
                        }
                    }
                    // Gem state
                    Compound[] snapshot = new Compound[_compounds.Count];

                    for (int i = 0; i < _compounds.Count; i++)
                    {
                        snapshot[i] = new Compound(_compounds[i]);
                    }
                    lock (_states)
                    {
                        _states.Add(new State(_timeMilliseconds, snapshot));
                    }
                    if (_timeMilliseconds >= _maxSimTimeMilliseconds) _on = false;
                }
                Debug.WriteLine("Køre ikke");
            });
        }
        private void BeregnKollision(Compound compound1, Compound compound2, Vector2 I)
        {
            if (I.Length() == 0) return;
            // Beregn kollision
            Vector2 i = Vector2.Normalize(I);

            float U1 = Vector2.Dot(compound1.Velocity, i);
            float U2 = Vector2.Dot(compound2.Velocity, i);

            double local_e_a = _bindingsenergiA + _bindingsenergiB > _bindingsenergiC + _bindingsenergiD ? _bindingsenergiA + _bindingsenergiB + _e_a : _bindingsenergiC + _bindingsenergiD + _e_a;

            float relativHastighed = U1 - U2;

            if (relativHastighed > 0)
            {
                Random r = new();
                double E_kin_total = (compound1.Mass * compound2.Mass * 1.66053906660e-27 / (compound1.Mass + compound2.Mass)) * relativHastighed * relativHastighed / 2;

                double E_kin1 = compound1.Mass * 1.66053906660e-27 * compound1.Velocity.LengthSquared() / 2;
                double E_kin2 = compound2.Mass * 1.66053906660e-27 * compound2.Velocity.LengthSquared() / 2;

                bool reacted = false;
                double E_ændring = 0;

                if (compound1.Type == Type.A && compound2.Type == Type.B && E_kin_total >= local_e_a - _bindingsenergiA - _bindingsenergiB)
                {
                    if (_ABToCD != 0 && r.Next(_ABToCD + 1) == 1)
                    {
                        reacted = true;
                        compound1.ChangeType(Type.C);
                        compound2.ChangeType(Type.D);
                        E_ændring = (_bindingsenergiA + _bindingsenergiB) - (_bindingsenergiC + _bindingsenergiD);
                    }
                }
                else if (compound1.Type == Type.B && compound2.Type == Type.A && E_kin_total >= local_e_a - _bindingsenergiA - _bindingsenergiB)
                {
                    if (_ABToCD != 0 && r.Next(_ABToCD + 1) == 1)
                    {
                        reacted = true;
                        compound1.ChangeType(Type.D);
                        compound2.ChangeType(Type.C);
                        E_ændring = (_bindingsenergiA + _bindingsenergiB) - (_bindingsenergiC + _bindingsenergiD);
                    }
                }
                else if (compound1.Type == Type.C && compound2.Type == Type.D && E_kin_total >= local_e_a - _bindingsenergiC - _bindingsenergiD)
                {
                    if (_CDToAB != 0 && r.Next(_CDToAB + 1) == 1)
                    {
                        reacted = true;
                        compound1.ChangeType(Type.A);
                        compound2.ChangeType(Type.B);
                        E_ændring = (_bindingsenergiC + _bindingsenergiD) - (_bindingsenergiA + _bindingsenergiB);
                    }
                }
                else if (compound1.Type == Type.D && compound2.Type == Type.C && E_kin_total >= local_e_a - _bindingsenergiC - _bindingsenergiD)
                {
                    if (_CDToAB != 0 && r.Next(_CDToAB + 1) == 1)
                    {
                        reacted = true;
                        compound1.ChangeType(Type.B);
                        compound2.ChangeType(Type.A);
                        E_ændring = (_bindingsenergiC + _bindingsenergiD) - (_bindingsenergiA + _bindingsenergiB);
                    }
                }

                if (reacted)
                {
                    // Koriger hastighed for ændring af masse, så energien bevares
                    double new_E_kin1 = E_kin1 + (E_kin1 / (E_kin1 + E_kin2)) * E_ændring;
                    double new_E_kin2 = E_kin2 + E_ændring * (E_kin2 / (E_kin1 + E_kin2));


                    Vector2 retning1 = Vector2.Normalize(compound1.Velocity);
                    Vector2 retning2 = Vector2.Normalize(compound2.Velocity);

                    compound1.Velocity = (float)Math.Sqrt(2 * new_E_kin1 / (compound1.Mass * 1.66053906660e-27)) * retning1;
                    compound2.Velocity = (float)Math.Sqrt(2 * new_E_kin2 / (compound2.Mass * 1.66053906660e-27)) * retning2;
                }
            }

            double u1 = Vector2.Dot(compound1.Velocity, i);
            double u2 = Vector2.Dot(compound2.Velocity, i);

            double v1 = (u1 * (compound1.Mass - compound2.Mass) + 2 * compound2.Mass * u2) / (compound1.Mass + compound2.Mass);
            double v2 = (u2 * (compound2.Mass - compound1.Mass) + 2 * compound1.Mass * u1) / (compound1.Mass + compound2.Mass);
            if (u1 - u2 >= 0f)
            {
                compound1.Velocity += (float)(v1 - u1) * i;
                compound2.Velocity += (float)(v2 - u2) * i;
            }

            // flyt stoffer ud ad hinanden
            float distance = compound1.Radius + compound2.Radius - I.Length();

            if (distance > 0)
            {
                compound1.Position -= i * distance * compound1.Radius / (compound1.Radius + compound2.Radius);
                compound2.Position += i * distance * compound2.Radius / (compound1.Radius + compound2.Radius);
            }
        }
    }
}
