using StringToExpression.GrammarDefinitions;
using System;
using System.Linq.Expressions;
using NUnit.Framework;
using StringToExpression;

namespace StringToExpression.Test
{
    public class ParserErrorTests
    {
        public readonly Language Language;

        public ParserErrorTests()
        {
            BracketOpenDefinition openBracket, logFn, errorFn;
            GrammarDefinition listDelimiter;
            Language = new(
                new OperatorDefinition(
                    "PLUS",
                    @"\+",
                    1,
                    new[] { RelativePosition.Left, RelativePosition.Right },
                    args => Expression.Add(args[0], args[1])),
                new OperatorDefinition(
                    "MULTIPLY",
                    @"\*",
                    1,
                    new[] { RelativePosition.Left, RelativePosition.Right },
                    args => Expression.Multiply(args[0], args[1])),
                logFn = new FunctionCallDefinition(
                    "LOG",
                    @"[Ll]og\(",
                    new[] { typeof(double), typeof(double) },
                    args => Expression.Call(ReflectionUtil<int>.Method(x => Math.Log(0, 0)), args)),
                errorFn = new FunctionCallDefinition(
                    "ERROR",
                    @"error\(",
                    new[] { typeof(double), typeof(double) },
                    args => { throw new NotImplementedException("I am a function error"); }),
                openBracket = new(
                    "OPENBRACKET",
                    @"\("),
                listDelimiter = new ListDelimiterDefinition(
                    "COMMA",
                    @"\,"),
                new BracketCloseDefinition(
                    "CLOSEBRACKET",
                    @"\)",
                    new[] { openBracket, logFn, errorFn },
                    listDelimiter),
                new OperandDefinition(
                    "NUMBER",
                    @"\d*\.?\d+?",
                    x => Expression.Constant(double.Parse(x))),
                new OperandDefinition(
                    "POOP",
                    @"💩",
                    x => { throw new NotImplementedException("I am an operand error"); }),
                new OperandDefinition(
                    "STRING",
                    @"'.*?'",
                    x => Expression.Constant(x)),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );
        }


        [Theory]
        [TestCase("2 + xxxx + 3", typeof(GrammarUnknownException), 4, 8)]
        [TestCase("2 + + 3", typeof(OperandExpectedException), 3, 3)]
        [TestCase("2 + 2 2 + 3", typeof(OperandUnexpectedException), 6, 7)]
        [TestCase("2 3", typeof(OperandUnexpectedException), 2, 3)]
        [TestCase("2 (3 * 4)", typeof(OperandUnexpectedException), 2, 9)]
        [TestCase("2 + 2, 3 * 3", typeof(ListDelimiterNotWithinBrackets), 5, 6)]
        [TestCase("2 + (2*3", typeof(BracketUnmatchedException), 4, 5)]
        [TestCase("2 + 2)*3", typeof(BracketUnmatchedException), 5, 6)]
        [TestCase("2 + (5 + (2 * 3) + 1", typeof(BracketUnmatchedException), 4, 5)]
        [TestCase(")", typeof(BracketUnmatchedException), 0, 1)]
        [TestCase("Log(", typeof(BracketUnmatchedException), 0, 4)]
        [TestCase("2 + (5 + 2,,4) + 1", typeof(OperandExpectedException), 11, 11)]
        [TestCase("2 + (5 + 2,  ) + 1", typeof(OperandExpectedException), 11, 13)]
        [TestCase("2 + ( , 5 + 2) + 1", typeof(OperandExpectedException), 5, 6)]
        [TestCase("2 + () + 1", typeof(OperandExpectedException), 5, 5)]
        [TestCase("2 + (,) + 1", typeof(OperandExpectedException), 6, 6)]
        [TestCase("", typeof(OperandExpectedException), 0, 0)]
        [TestCase("*", typeof(OperandExpectedException), 1, 1)]
        [TestCase("Log(1024,2,2)", typeof(FunctionArgumentCountException), 4, 12)]
        [TestCase("Log(1024)", typeof(FunctionArgumentCountException), 4, 8)]
        [TestCase("Log(1024,'2')", typeof(FunctionArgumentTypeException), 9, 12)]
        [TestCase("2 + '2'", typeof(OperationInvalidException), 0, 7)]
        [TestCase("2 + error(2,3)", typeof(OperationInvalidException), 4, 13)]
        [TestCase("2 + 💩 * 3", typeof(OperationInvalidException), 4, 6)] //also interesting as double-byte unicode have length 2
        public void When_invalid_should_throw_with_indication_of_error_location(string text, Type exceptionType, int errorStart, int errorEnd)
        {
            var exception = Assert.Throws(exceptionType, () => Language.Parse<double>(text));

            if (exception is not ParseException parseException)
                throw new AssertionException("Thrown exception should be descendant from ParseException.");
            Assert.AreEqual(errorStart, parseException.ErrorSegment.Start);
            Assert.AreEqual(errorEnd, parseException.ErrorSegment.End);
        }
    }
}
