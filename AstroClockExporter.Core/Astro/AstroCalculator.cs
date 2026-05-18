using System.Diagnostics;
using AASharp;
using AstroClockExporter.Core.Configuration.DTO;

namespace AstroClockExporter.Core.Astro;

// Sun/moon position and rise-transit-set calculations via AASharp (port of Meeus's Astronomical Algorithms).
// Conventions used by AASharp / AA+:
//   - Longitude is positive WEST (so 21° E is supplied as -21).
//   - JD is supplied in UT for sidereal/rise-set math, and in TT (= UT + ΔT) for position math.
//   - AASRiseTransitSet2 returns event JDs in UT directly.
internal sealed class AstroCalculator : IAstroCalculator
{
    private const double H0Sun = -0.5833;  // standard refraction + solar semidiameter
    private const double RiseSetStepHours = 0.007;
    private const double EarthRadiusKm = 6378.14;
    private const double AuKm = 149_597_870.7;

    public AstroReading Calculate(Location location, DateTime utcNow)
    {
        var sw = Stopwatch.GetTimestamp();

        // AASharp expects longitude positive WEST; user supplies positive East.
        var longitudeWest = -location.Longitude;
        var latitude = location.Latitude;
        var height = location.Elevation;

        // JD for "now" (UT).
        var nowDate = new AASDate(utcNow.Year, utcNow.Month, utcNow.Day,
            utcNow.Hour, utcNow.Minute, utcNow.Second + utcNow.Millisecond / 1000.0, true);
        var jdUt = nowDate.Julian;
        var deltaT = AASDynamicalTime.DeltaT(jdUt);
        var jdTt = jdUt + deltaT / 86400.0;

        // ----- Current sun position (topocentric horizontal: altitude / azimuth) -----
        var (sunAlt, sunAz) = SunHorizontal(jdUt, jdTt, longitudeWest, latitude);

        // ----- Current moon position + phase / illumination -----
        var (moonAlt, moonAz, moonDistKm, moonPhaseDeg, moonIllum) =
            MoonHorizontalAndPhase(jdUt, jdTt, longitudeWest, latitude);

        // ----- Sun events from now → +24h -----
        var sunEvents = AASRiseTransitSet2.Calculate(
            jdUt, jdUt + 1.0, AASRiseSetObject.SUN,
            longitudeWest, latitude, H0Sun, height, RiseSetStepHours, false);

        // ----- Moon events from now → +24h (CalculateMoon handles parallax/refraction internally) -----
        var moonEvents = AASRiseTransitSet2.CalculateMoon(
            jdUt, jdUt + 1.0, longitudeWest, latitude, height, RiseSetStepHours);

        var elapsedSeconds = Stopwatch.GetElapsedTime(sw).TotalSeconds;

        return new AstroReading
        {
            SunAltitudeDegrees = sunAlt,
            SunAzimuthDegrees = sunAz,
            MoonAltitudeDegrees = moonAlt,
            MoonAzimuthDegrees = moonAz,
            MoonIlluminationFraction = moonIllum,
            MoonPhaseAngleDegrees = moonPhaseDeg,
            MoonDistanceKilometres = moonDistKm,

            SunriseUnix = FirstEventUnix(sunEvents, AASRiseTransitSetDetails2.Type.Rise),
            SunsetUnix = FirstEventUnix(sunEvents, AASRiseTransitSetDetails2.Type.Set),
            SolarNoonUnix = FirstTransitUnix(sunEvents, latitude),
            CivilDawnUnix = FirstEventUnix(sunEvents, AASRiseTransitSetDetails2.Type.EndCivilTwilight),
            CivilDuskUnix = FirstEventUnix(sunEvents, AASRiseTransitSetDetails2.Type.StartCivilTwilight),
            NauticalDawnUnix = FirstEventUnix(sunEvents, AASRiseTransitSetDetails2.Type.EndNauticalTwilight),
            NauticalDuskUnix = FirstEventUnix(sunEvents, AASRiseTransitSetDetails2.Type.StartNauticalTwilight),
            AstronomicalDawnUnix = FirstEventUnix(sunEvents, AASRiseTransitSetDetails2.Type.EndAstronomicalTwilight),
            AstronomicalDuskUnix = FirstEventUnix(sunEvents, AASRiseTransitSetDetails2.Type.StartAstronomicalTwilight),

            MoonriseUnix = FirstEventUnix(moonEvents, AASRiseTransitSetDetails2.Type.Rise),
            MoonsetUnix = FirstEventUnix(moonEvents, AASRiseTransitSetDetails2.Type.Set),
            MoonTransitUnix = FirstTransitUnix(moonEvents, latitude),

            CalculationSeconds = elapsedSeconds,
        };
    }

    // ------- Helpers -------

    private static (double Altitude, double Azimuth) SunHorizontal(double jdUt, double jdTt, double longitudeWest, double latitude)
    {
        var lambda = AASSun.ApparentEclipticLongitude(jdTt, false);
        var beta = AASSun.ApparentEclipticLatitude(jdTt, false);
        var epsilon = AASNutation.TrueObliquityOfEcliptic(jdTt);
        var eq = AASCoordinateTransformation.Ecliptic2Equatorial(lambda, beta, epsilon);
        var gast = AASSidereal.ApparentGreenwichSiderealTime(jdUt); // hours
        var lhaHours = AASCoordinateTransformation.MapTo0To24Range(gast - longitudeWest / 15.0 - eq.X);
        var horizontal = AASCoordinateTransformation.Equatorial2Horizontal(lhaHours, eq.Y, latitude);
        // horizontal.X = Az measured WEST from SOUTH; convert to clockwise-from-north.
        var azNorth = AASCoordinateTransformation.MapTo0To360Range(horizontal.X + 180.0);
        return (horizontal.Y, azNorth);
    }

    private static (double Altitude, double Azimuth, double DistanceKm, double PhaseAngleDeg, double IlluminatedFraction)
        MoonHorizontalAndPhase(double jdUt, double jdTt, double longitudeWest, double latitude)
    {
        var lambda = AASMoon.EclipticLongitude(jdTt);
        var beta = AASMoon.EclipticLatitude(jdTt);
        var distanceKm = AASMoon.RadiusVector(jdTt);
        var epsilon = AASNutation.TrueObliquityOfEcliptic(jdTt);
        var eq = AASCoordinateTransformation.Ecliptic2Equatorial(lambda, beta, epsilon);
        var gast = AASSidereal.ApparentGreenwichSiderealTime(jdUt);
        var lhaHours = AASCoordinateTransformation.MapTo0To24Range(gast - longitudeWest / 15.0 - eq.X);
        var horizontal = AASCoordinateTransformation.Equatorial2Horizontal(lhaHours, eq.Y, latitude);
        var azNorth = AASCoordinateTransformation.MapTo0To360Range(horizontal.X + 180.0);

        // Sun position (geocentric) for phase / illumination
        var sunLambda = AASSun.ApparentEclipticLongitude(jdTt, false);
        var sunBeta = AASSun.ApparentEclipticLatitude(jdTt, false);
        var sunRAu = AASEarth.RadiusVector(jdTt, false);
        var sunDistKm = sunRAu * AuKm;
        var sunEq = AASCoordinateTransformation.Ecliptic2Equatorial(sunLambda, sunBeta, epsilon);

        var elongation = AASMoonIlluminatedFraction.GeocentricElongation(eq.X, eq.Y, sunEq.X, sunEq.Y);
        // PhaseAngle signature: (elongation, EarthObjectDistance, EarthSunDistance) — Earth-Moon then Earth-Sun.
        var phaseAngle = AASMoonIlluminatedFraction.PhaseAngle(elongation, distanceKm, sunDistKm);
        var illum = AASMoonIlluminatedFraction.IlluminatedFraction(phaseAngle);

        _ = EarthRadiusKm; // referenced if Meeus-parallax path is enabled later
        return (horizontal.Y, azNorth, distanceKm, phaseAngle, illum);
    }

    private static double FirstEventUnix(IEnumerable<AASRiseTransitSetDetails2> events, AASRiseTransitSetDetails2.Type type)
    {
        foreach (var e in events)
            if (e.type == type)
                return JulianToUnix(e.JD);
        return double.NaN;
    }

    // "Solar/lunar noon" is the transit on the side of the meridian closer to the local zenith:
    //   - Northern hemisphere: southern transit
    //   - Southern hemisphere: northern transit
    private static double FirstTransitUnix(IEnumerable<AASRiseTransitSetDetails2> events, double latitude)
    {
        var preferred = latitude >= 0
            ? AASRiseTransitSetDetails2.Type.SouthernTransit
            : AASRiseTransitSetDetails2.Type.NorthernTransit;
        return FirstEventUnix(events, preferred);
    }

    private static double JulianToUnix(double jd) => (jd - 2440587.5) * 86400.0;
}
