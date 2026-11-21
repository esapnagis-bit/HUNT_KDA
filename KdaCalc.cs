using System.Globalization;

namespace HuntKdaCalculator;

readonly record struct GrowStep(long Target100, long Kills, string Exact);

sealed record KdaResult(
    bool Infinite,
    long Display100,          // отображаемое KDA в сотых (155 = 1.55)
    string Exact,             // точное KDA, 4 знака (усечение)
    GrowStep[] Up,
    long? DownTarget100,      // null, если падение невозможно (KDA уже 0.00)
    long DownDeaths,
    string DownExact);

/// <summary>Все вычисления в целых числах — без ошибок плавающей точки на границах округления.</summary>
static class KdaCalc
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Fmt2(long h100) => (h100 / 100m).ToString("0.00", Inv);

    // 4 знака усечением, чтобы значение у границы (1.5449...) не выглядело как 1.5450
    static string Fmt4(long s, long d) => (s * 10000 / d / 10000m).ToString("0.0000", Inv);

    public static KdaResult Compute(long kills, long deaths, long assists)
    {
        long s = kills + assists, d = deaths;
        if (d == 0)
            return new KdaResult(true, 0, "", Array.Empty<GrowStep>(), null, 0, "");

        // Round(S/D, 2, AwayFromZero) в сотых: floor((200*S + D) / (2*D))
        long dh = (200 * s + d) / (2 * d);

        // Рост: нужно S'/D >= (t - 0.005)  <=>  200*S' >= (2t - 1)*D
        var up = new GrowStep[3];
        for (int i = 1; i <= 3; i++)
        {
            long t = dh + i;
            long n = (2 * t - 1) * d - 200 * s;
            long x = n <= 0 ? 0 : (n + 199) / 200;
            up[i - 1] = new GrowStep(t, x, Fmt4(s + x, d));
        }

        // Падение: нужно S/D' < (dh - 0.005)  <=>  (2*dh - 1)*D' > 200*S
        long? downTarget = null;
        long y = 0;
        string downExact = "";
        if (dh >= 1)
        {
            long m = 2 * dh - 1;
            y = 200 * s / m + 1 - d;
            downTarget = dh - 1;
            downExact = Fmt4(s, d + y);
        }

        return new KdaResult(false, dh, Fmt4(s, d), up, downTarget, y, downExact);
    }
}
