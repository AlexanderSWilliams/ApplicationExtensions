using System.Security.Cryptography;

namespace Application.Math
{
    public static class Random
    {
        public static int GetRandomNumber(int minValue, int maxValue)
        {
            var seed = GetSeed();
            var random = new System.Random(seed);
            return random.Next(minValue, maxValue);
        }

        public static int GetSeed()
        {
            var randomBytes = new byte[4];

            // Generate 4 random bytes.
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(randomBytes);

            // Convert 4 bytes into a 32-bit integer value.
            int seed = (randomBytes[0] & 0x7f) << 24 |
                        randomBytes[1] << 16 |
                        randomBytes[2] << 8 |
                        randomBytes[3];

            return seed;
        }
    }
}