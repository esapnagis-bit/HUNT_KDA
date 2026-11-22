using System.Globalization;

namespace HuntKdaCalculator;

readonly record struct GrowStep(long Target100, long Kills);

sealed record KdaResult(
    bool Infinite,
    long Display100,          // отображаемое KDA в сотых (155 = 1.55)
    GrowStep[] Up,
    long? DownTarget100,      // null, если падение невозможно (KDA уже 0.00)
    long DownDeaths);

/// <summary>Все вычисления в целых числах — без ошибок плавающей точки на границах округления.</summary>
static class KdaCalc
{
    public static string Fmt2(long h100) => (h100 / 100m).ToString("0.00", CultureInfo.InvariantCulture);

    public static KdaResult Compute(long kills, long deaths, long assists)
    {
        long s = kills + assists, d = deaths;
        if (d == 0)
            return new KdaResult(true, 0, Array.Empty<GrowStep>(), null, 0);

        // Round(S/D, 2, AwayFromZero) в сотых: floor((200*S + D) / (2*D))
        long dh = (200 * s + d) / (2 * d);

        // Рост: нужно S'/D >= (t - 0.005)  <=>  200*S' >= (2t - 1)*D
        var up = new GrowStep[3];
        for (int i = 1; i <= 3; i++)
        {
            long t = dh + i;
            long n = (2 * t - 1) * d - 200 * s;
            long x = n <= 0 ? 0 : (n + 199) / 200;
            up[i - 1] = new GrowStep(t, x);
        }

        // Падение: нужно S/D' < (dh - 0.005)  <=>  (2*dh - 1)*D' > 200*S
        long? downTarget = null;
        long y = 0;
        if (dh >= 1)
        {
            long m = 2 * dh - 1;
            y = 200 * s / m + 1 - d;
            downTarget = dh - 1;
        }

        return new KdaResult(false, dh, up, downTarget, y);
    }
}
