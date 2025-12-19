using System.Numerics;
using System.Windows.Media;

namespace KemiK_0_3.Sim
{
    internal class Compound
    {
        const double k_amuToRadiusFactor = 3; // nm
        private static SolidColorBrush ABrush { get; }
        private static SolidColorBrush BBrush { get; }
        private static SolidColorBrush CBrush { get; }
        private static SolidColorBrush DBrush { get; }
        private static SolidColorBrush XBrush { get; }
        private Vector2 _velocity;
        private Vector2 _position;
        public float Radius { get; set; }                                                   // m
        public float Mass { get; set; }                                                     // amu
        public Vector2 Velocity { get { return _velocity; } set { _velocity = value; } }    // m/s
        public Vector2 Position { get { return _position; } set { _position = value; } }    // m
        public Type Type { get; set; }
        public SolidColorBrush Brush
        {
            get
            {
                if (Type == Type.A) return Compound.ABrush;
                if (Type == Type.B) return Compound.BBrush;
                if (Type == Type.C) return Compound.CBrush;
                if (Type == Type.D) return Compound.DBrush;
                return Compound.XBrush;
            }
        }
        static Compound()
        {
            ABrush = new(Colors.Maroon);
            BBrush = new(Colors.Navy);
            CBrush = new(Colors.DarkSeaGreen);
            DBrush = new(Colors.DarkOrange);
            XBrush = new(Colors.White);

            ABrush.Freeze();
            BBrush.Freeze();
            CBrush.Freeze();
            DBrush.Freeze();
            XBrush.Freeze();
        }
        public void Move(float deltaTimeMilliseconds)
        {
            _position += _velocity * deltaTimeMilliseconds / 1000;
        }
        public void CheckCollisionTop()
        {
            if (_position.Y - Radius < 0)
            {
                _position.Y += Radius - _position.Y;
                if (_velocity.Y < 0) _velocity.Y *= -1;
            }
        }
        public void CheckCollisionBottom(Vector2 simSize)
        {
            if (_position.Y + Radius > simSize.Y)
            {
                _position.Y += simSize.Y - Radius - _position.Y;
                if (_velocity.Y > 0) _velocity.Y *= -1;
            }
        }
        public void CheckCollisionLeft()
        {
            if (_position.X - Radius < 0)
            {
                _position.X += Radius - _position.X;
                if (_velocity.X < 0) _velocity.X *= -1;
            }
        }
        public void CheckCollisionRight(Vector2 simSize)
        {
            if (_position.X + Radius > simSize.X)
            {
                _position.X += simSize.X - Radius - _position.X;
                if (_velocity.X > 0) _velocity.X *= -1;
            }
        }
        public Compound(Vector2 position, Vector2 velocity, Type type)
        {
            _position = position;
            _velocity = velocity;
            Type = type;
            Radius = (float)(Math.Cbrt((float)type) * k_amuToRadiusFactor);
            Mass = (float)type;
        }
        public Compound(Compound compound)
        {
            _position = new(compound._position.X, compound._position.Y);
            _velocity = new(compound._velocity.X, compound._velocity.Y);
            Type = compound.Type;
            Radius = compound.Radius;
            Mass = compound.Mass;
        }
        public void ChangeType(Type newType)
        {
            Type = newType;
            Radius = (float)(Math.Cbrt((float)newType) * k_amuToRadiusFactor);
            Mass = (float)newType;
        }
    }
}