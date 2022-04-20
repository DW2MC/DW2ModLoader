using FastExpressionCompiler.LightExpression;
using StringToExpression.LanguageDefinitions;
using NUnit.Framework;

namespace StringToExpression.Test.Languages.Arithmetic
{
    public class Parameter
    {
        public int FavouriteNumber { get; set; }

        public Limit Limits { get; set; }
    }

    public class Limit
    {
        public double Min { get; set; }
        public double Max { get; set; }
    }

    public class ArithmeticLangaugeTests
    {
        [Theory]
        [TestCase("1 + 1", 2)]
        [TestCase("2 + 2 * 5", 12)]
        [TestCase("2 + -2 * 5", -8)]
        [TestCase("(2 + 2) * 5", 20)]
        [TestCase("4 - 2 * 5", -6)]
        [TestCase("(4 - 2) * 5", 10)]
        [TestCase("2.5 * 4", 10)]
        [TestCase("2.5 * 3", 7.5)]
        [TestCase("9 / 10", 0.9)]
        [TestCase("10 / 9", 1.111d)]
        [TestCase("10 / 5 * 2", 4)]
        [TestCase("10 % 3", 1)]
        [TestCase("Pi", 3.14159)]
        [TestCase("sin(Pi)", 0)]
        [TestCase("Sin(Pi * 1 / 2)", 1)]
        [TestCase("SIN(Pi * 1 / 4)", 0.707)]
        [TestCase("cos(Pi)", -1)]
        [TestCase("Cos(Pi * 1 / 2)", 0)]
        [TestCase("COS(Pi * 1 / 4)", 0.707)]
        [TestCase("tan(Pi)", 0)]
        [TestCase("sqrt(12 * 3)", 6)]
        [TestCase("SQRT(sqrt(81))", 3)]
        [TestCase("Pow(12, 2)", 144)]
        public void When_no_parameters_should_evaluate(string math, double result)
        {
            var language = new ArithmeticLanguage();
            var function = language.Parse(math).CompileFast();

            Assert.AreEqual(result, function(), 0.0005);

        }

        [Theory]
        [TestCase("FavouriteNumber", 7)]
        [TestCase("FavouriteNumber + 3", 10)]
        [TestCase("Limits.Min + Limits.Max + FavouriteNumber", 7.5)]
        public void When_parameters_should_evaluate(string math, double result)
        {
            var language = new ArithmeticLanguage();
            var function = language.Parse<Parameter>(math).CompileFast();
            var parameter = new Parameter
            {
                FavouriteNumber = 7,
                Limits = new()
                {
                    Min = -1.0,
                    Max = 1.5
                }
            };
            Assert.AreEqual(result, function(parameter));

        }
    }
}
