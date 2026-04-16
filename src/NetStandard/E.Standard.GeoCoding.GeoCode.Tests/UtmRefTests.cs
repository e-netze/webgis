using E.Standard.GeoCoding.GeoCode;
using System.Text;
using Xunit;

namespace E.Standard.GeoCoding.GeoCode.Tests
{
    public class UtmRefTests
    {
        [Theory]
        [InlineData(15.438125962641667, 47.07103890521332, 5, "33TWN3326413151")] //Graz Hauptplatz
        [InlineData(14.307720453455207, 46.623980357285326, 5, "33TVM4700163612")] //Klagenfurt Hauptplatz
        [InlineData(16.519343420988875, 47.84646732033266, 5, "33TXP1367000353")] //Eisenstadt Schloss Esterhazy
        [InlineData(16.37794002648094, 48.174337186599985, 5, "33UXP0244036595")] //Wien Reumannplatz
        [InlineData(15.622919741212078, 48.20510127908814, 5, "33UWP4628239284")] //StPölten Hauptplatz
        [InlineData(14.286442730625943, 48.305848988661154, 5, "33UVP4708750541")] //Linz Hauptplatz
        [InlineData(9.738094752637286, 47.50541626087741, 5, "32TNT5558261595")] //Bregenz Seebühne
        [InlineData(11.390938277429681, 47.268665068219995, 5, "32TPT8085537793")] //Innsbruck Innbrücke
        [InlineData(13.046856533816197, 47.797850344329134, 5, "33TUN5373895679")] //Salzburg Dom
        [InlineData(139.74548114088196, 35.658545129201755, 5, "54SUE8644446801")] //Tokyo Tower
        [InlineData(-74.04453160267946, 40.689264937743346, 5, "18TWL8073304702")] //NY Freiheitsstatue
        [InlineData(-43.21047939725911, -22.951929690279, 5, "23KPQ8347860683")] //Rio Jesus Statue
        [InlineData(19.999960398451993, -34.83252785506585, 5, "34HDG0855745072")] //Südlichster Punkt Afrikas
        [InlineData(55.254781154025444, 80.79430189784442, 5, "40XDQ6883470712")] //Franz Josef Land
        public void Encode_ShouldReturnExpectedString(double lon, double lat, int precision, string expected)
        {
            var utm = new UtmRef();

            string result = utm.Encode(lon, lat, precision);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(15.438125962641667, 47.07103890521332, 5, "33TWN3326413151")] //Graz Hauptplatz
        [InlineData(14.307720453455207, 46.623980357285326, 5, "33TVM4700163612")] //Klagenfurt Hauptplatz
        [InlineData(16.519343420988875, 47.84646732033266, 5, "33TXP1367000353")] //Eisenstadt Schloss Esterhazy
        [InlineData(16.37794002648094, 48.174337186599985, 5, "33UXP0244036595")] //Wien Reumannplatz
        [InlineData(15.622919741212078, 48.20510127908814, 5, "33UWP4628239284")] //StPölten Hauptplatz
        [InlineData(14.286442730625943, 48.305848988661154, 5, "33UVP4708750541")] //Linz Hauptplatz
        [InlineData(9.738094752637286, 47.50541626087741, 5, "32TNT5558261595")] //Bregenz Seebühne
        [InlineData(11.390938277429681, 47.268665068219995, 5, "32TPT8085537793")] //Innsbruck Innbrücke
        [InlineData(13.046856533816197, 47.797850344329134, 5, "33TUN5373895679")] //Salzburg Dom
        [InlineData(139.74548114088196, 35.658545129201755, 5, "54SUE8644446801")] //Tokyo Tower
        [InlineData(-74.04453160267946, 40.689264937743346, 5, "18TWL8073304702")] //NY Freiheitsstatue
        [InlineData(-43.21047939725911, -22.951929690279, 5, "23KPQ8347860683")] //Rio Jesus Statue
        [InlineData(19.999960398451993, -34.83252785506585, 5, "34HDG0855745072")] //Südlichster Punkt Afrikas
        [InlineData(55.254781154025444, 80.79430189784442, 5, "40XDQ6883470712")] //Franz Josef Land
        public void Encode_GeoLocation_ShouldReturnExpectedString(double lon, double lat, int precision, string expected)
        {
            var utm = new UtmRef();

            var loc = new GeoLocation { Latitude = lat, Longitude = lon };

            string result = utm.Encode(loc, precision);

            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> InvalidEncodeData =>
        new List<object[]>
        {
            new object[] { double.NaN, 45.0, 3, "Latitude or longitude cannot be NaN or Infinity" },
            new object[] { 12.0, double.NaN, 3, "Latitude or longitude cannot be NaN or Infinity" },
            new object[] { double.PositiveInfinity, 12.0, 3, "Latitude or longitude cannot be NaN or Infinity" },
            new object[] { 12.0, double.NegativeInfinity, 3, "Latitude or longitude cannot be NaN or Infinity" },

            new object[] { 10.0, -81.0, 3, "UTM/MGRS not defined beyond -80 to 84 degrees latitude." },
            new object[] { 10.0,  85.0, 3, "UTM/MGRS not defined beyond -80 to 84 degrees latitude." },

            new object[] { -181.0, 10.0, 3, "Longitude value not in [-180,180]" },
            new object[] {  181.0, 10.0, 3, "Longitude value not in [-180,180]" },

            new object[] { 10.0, 10.0, 0, "Precision value must be between 1 and 5" },
            new object[] { 10.0, 10.0, 6, "Precision value must be between 1 and 5" },
        };

        [Theory]
        [MemberData(nameof(InvalidEncodeData))]
        public void Encode_ShouldReturnErrorString_ForInvalidInput(double lon, double lat, int precision, string expectedError)
        {
            var utm = new UtmRef();

            var result = utm.Encode(lon, lat, precision);

            Assert.Equal(expectedError, result);
        }


        [Theory]
        [InlineData("33TWN3326413151", 15.43811826, 47.07103372)] //Graz Hauptplatz
        [InlineData("33TVM4700163612", 14.30772025, 46.62397565)] //Klagenfurt Hauptplatz
        [InlineData("33TXP1367000353", 16.51933328, 47.84646411)] //Eisenstadt Schloss Esterhazy
        [InlineData("33UXP0244036595", 16.37793642, 48.17433377)] //Wien Reumannplatz
        [InlineData("33UWP4628239284", 15.62291391, 48.20509805)] //StPölten Hauptplatz
        [InlineData("33UVP4708750541", 14.28643774, 48.30584884)] //Linz Hauptplatz
        [InlineData("32TNT5558261595", 9.73808673, 47.50541037)] //Bregenz Seebühne
        [InlineData("32TPT8085537793", 11.39093579, 47.26866350)] //Innsbruck Innbrücke
        [InlineData("33TUN5373895679", 13.04684683, 47.79784493)] //Salzburg Dom
        [InlineData("54SUE8644446801", 139.74547167, 35.65853624)] //Tokyo Tower
        [InlineData("18TWL8073304702", -74.04453309, 40.68926184)] //NY Freiheitsstatue
        [InlineData("23KPQ8347960684", -43.21047955, -22.95192738)] //Rio Jesus Statue 
        [InlineData("34HDG0855745072", 19.99995410, -34.83253560)] //Südlichster Punkt Afrikas 
        [InlineData("40XDQ6883470712", 55.25473256, 80.79429558)] //Franz Josef Land
        [InlineData("58CEU3921558104", 166.66891110859723, -77.8473804048247)] //McMurdo Station
        [InlineData("17MQV7672675026", -78.51389162080228, -0.22572743468148287)] //Quito Ecuador
        [InlineData("27WVM5482612664", -21.928155561356238, 64.13749172324412)] //Reykjavik
        [InlineData("48NUG7141443747", 103.84421525471014, 1.3002615919722442)] //Singapore
        [InlineData("6VUN4536489378", -149.87858392498504, 61.208155842162114)] //Anchorage

        public void Decode_ShouldReturnExpectedString(string utmCode, double exptedLon, double exptctedLat)
        {
            var utm = new UtmRef();

            var result = utm.Decode(utmCode);

            Assert.InRange(result.Longitude, exptedLon - 0.0001, exptedLon + 0.0001);
            Assert.InRange(result.Latitude, exptctedLat - 0.0001, exptctedLat + 0.0001);
        }

        [Theory]
        [InlineData(null, "MGRS code cannot be null or empty")]
        [InlineData("", "MGRS code cannot be null or empty")]
        [InlineData("   ", "MGRS code cannot be null or empty")]
        [InlineData("\t", "MGRS code cannot be null or empty")]
        [InlineData("A", "Invalid MGRS code: missing zone number")]
        [InlineData("ABC", "Invalid MGRS code: missing zone number")]
        [InlineData("ABCDEF", "Invalid MGRS code: missing zone number")]
        [InlineData("0", "Invalid UTM zone:")]
        [InlineData("61", "Invalid UTM zone:")]
        [InlineData("99", "Invalid UTM zone:")]
        [InlineData("-1", "Invalid MGRS code:")]
        [InlineData("100", "Invalid UTM zone:")]
        [InlineData("31", "Invalid MGRS code: missing latitude band")]
        [InlineData("31U", "Invalid MGRS code: missing grid square letters")]
        [InlineData("31UE", "Invalid MGRS code: missing grid square letters")]
        [InlineData("31UDQ1", "Invalid MGRS code: easting and northing must have equal precision")]
        [InlineData("31UDQ123", "Invalid MGRS code: easting and northing must have equal precision")]
        [InlineData("31UDQ12345", "Invalid MGRS code: easting and northing must have equal precision")]
        [InlineData("31UDQ", "Invalid precision:")]
        [InlineData("31UDQ12345678901", "Invalid MGRS code:")]
        public void Decode_InvalidInput_ReturnsExpectedErrorMessage(string? geoCode, string expectedErrorSubstring)
        {
            var utm = new UtmRef();

            var result = utm.Decode(geoCode!);

            Assert.NotNull(result);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains(expectedErrorSubstring, result.ErrorMessage);
        }

        [Theory]
        [InlineData("33TWN3326413151")]
        [InlineData("33TVM4700163612")]
        [InlineData("33TXP1367000353")]
        [InlineData("33UXP0244036595")]
        [InlineData("33UWP4628239284")]
        [InlineData("33UVP4708750541")]
        [InlineData("32TNT5558261595")]
        [InlineData("32TPT8085537793")]
        [InlineData("33TUN5373895679")]
        [InlineData("54SUE8644446801")]
        [InlineData("18TWL8073304702")]
        [InlineData("23KPQ8347960684")]
        [InlineData("34HDG0855745072")]
        [InlineData("40XDQ6883470712")]
        [InlineData("58CEU3921558104")]
        [InlineData("17MQV7672675026")]
        [InlineData("27WVM5482612664")]
        [InlineData("48NUG7141443747")]
        [InlineData("6VUN4536489378")]
        [InlineData("6VUN45364893")]
        [InlineData("6VUN4536")]
        [InlineData("6VUN45")]
        public void IsValidGeoCode_ShouldReturnTrue(string utmCode)
        {
            var utm = new UtmRef();

            var result = utm.IsValidGeoCode(utmCode);

            Assert.True(result);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("A")]
        [InlineData("ABC")]
        [InlineData("ABCDEF")]
        [InlineData("0")]
        [InlineData("61")]
        [InlineData("99")]
        [InlineData("-1")]
        [InlineData("100")]
        [InlineData("31")]
        [InlineData("31U")]
        [InlineData("31UE")]
        [InlineData("31UDQ1")]
        [InlineData("31UDQ123")]
        [InlineData("31UDQ12345")]
        [InlineData("31UDQ")]
        [InlineData("31UDQ12345678901")]
        public void IsValidGeoCode_ShouldReturnFalse(string? utmCode)
        {
            var utm = new UtmRef();

            var result = utm.IsValidGeoCode(utmCode!);

            Assert.False(result);
        }
    }
}
