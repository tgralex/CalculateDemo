using CalculateDemo.Server.Classes;

namespace CalculateDemo.ServerTests.Controllers
{
    [TestClass]
    public class CalculationTests
    {
        [TestMethod]
        public void SuccessCalculationTest()
        {
            var successStrArray = new List<(string str, float res)>
            {
                ("12+14/2", 19.0f),
                ("12 +     14/2   ", 19.0f),
                ("12 + 31 - 5 * 2", 33f),
                ("12 - 5 * 2", 2f),
                ("1.2 * 3 - 0.6 * 5", 0.6f),
                ("12 - 5 * 2.4", 0f),
                ("(12 - 2) * 2.4", 24f),
                ("(12 - 2) / (2 + 3)", 2f)
            };
            successStrArray.ForEach(x =>
            {
                var calc = new Calculation(x.str);
                Assert.IsTrue(calc.CalculationSuccess, $"Calculation of '{x.str}' is unsuccessful");
                Assert.AreEqual(x.res, calc.CalculationResult.GetValueOrDefault(), 0.00001f,$"Calculations of '{x.str}' is incorrect. Expected: {x.res}, Actual:{calc.CalculationResult}");
            });
        }

        [TestMethod]
        public void FailureCalculationTest()
        {
            var failureStrArray = new List<string>
            {
                "12 - 5 / 0",
                "12+14/2a - x",
                "12 + 31 - 5 * 2 / Pi",
                "12 * 3 - 6 * 5 Ln(10)",
                "(12 - 2) / (2 + 3))",
                "(12 - 2 / (2 + 3)",
                "(12 - 2 / 2 + 3",
                "asdfghjkl",
                new('9', 100),
                "",
                "/",
                "**.-+"
            };
            failureStrArray.ForEach(x =>
            {
                var calc = new Calculation(x);
                Assert.IsFalse(calc.CalculationSuccess, $"Calculation of '{x}' is somehow successful");
            });
        }

    }
}