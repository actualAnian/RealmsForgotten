namespace RealmsForgotten.Alchemy.Bombs
{
    public class OnProjectilePassedTextProvider
    {
        float _accumulator = 0f;
        bool _showText = false;
        public float ShowTimer { get; set; } = 5f;
        public bool ShowText => _showText;
        public void OnProjectilePassed()
        {
            _showText = true;
            _accumulator = 0f;
        }
        public void OnTick(float dt)
        {
            _accumulator += dt;
            if (_accumulator > ShowTimer) _showText = false;
        }
    }
}
