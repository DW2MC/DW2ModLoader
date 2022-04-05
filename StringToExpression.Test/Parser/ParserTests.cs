using StringToExpression.GrammarDefinitions;
using System;
using System.Linq.Expressions;
using NUnit.Framework;
using StringToExpression;

namespace StringToExpression.Test
{
    public class ParserTests
    {
        [Test]
        public void Should_parse_basic_expression()
        {
            var language = new Language(
                new OperatorDefinition(
                    "PLUS",
                    @"\+",
                    new[] { RelativePosition.Left, RelativePosition.Right },
                    args => Expression.Add(args[0], args[1])),
                new OperandDefinition(
                    "NUMBER",
                    @"\d*\.?\d+?",
                    x => Expression.Constant(double.Parse(x))),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );

            var expression = language.Parse<double>("1 + 2 + 3 + 5");
            Assert.AreEqual(11, expression.Compile()());
        }

        [Test]
        public void When_too_many_operaters_should_throw()
        {
            var language = new Language(
                new OperatorDefinition(
                    "PLUS",
                    @"\+",
                    new[] { RelativePosition.Left, RelativePosition.Right },
                    args => Expression.Add(args[0], args[1])),
                new OperandDefinition(
                    "NUMBER",
                    @"\d*\.?\d+?",
                    x => Expression.Constant(double.Parse(x))),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );

            var exception = Assert.Throws<OperandExpectedException>(() => language.Parse<double>("1 + + 5"))!;
            Assert.AreEqual("1 + [+] 5", exception.OperatorSubstring.Highlight());
            Assert.AreEqual("1 + []+ 5", exception.ExpectedOperandSubstring.Highlight());
        }

        [Test]
        public void When_too_many_operand_should_throw()
        {
            var language = new Language(
                new OperatorDefinition(
                    "PLUS",
                    @"\+",
                    new[] { RelativePosition.Left, RelativePosition.Right },
                    args => Expression.Add(args[0], args[1])),
                new OperandDefinition(
                    "NUMBER",
                    @"\d*\.?\d+?",
                    x => Expression.Constant(double.Parse(x))),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );

            var exception = Assert.Throws<OperandUnexpectedException>(() => language.Parse<double>("1 + 5 5"))!;
            Assert.AreEqual("1 [+] 5 5", exception.OperatorSubstring.Highlight());
            Assert.AreEqual("1 + 5 [5]", exception.UnexpectedOperandSubstring.Highlight());
        }


        [Test]
        public void Should_obey_operator_precedence()
        {
            var language = new Language(
                new OperatorDefinition(
                    "PLUS",
                    @"\+",
                    2,
                    new[] { RelativePosition.Left, RelativePosition.Right },
                    args => Expression.Add(args[0], args[1])),
                new OperatorDefinition(
                    "MULTIPLY",
                    @"\*",
                    1,
                    new[] { RelativePosition.Left, RelativePosition.Right },
                    args => Expression.Multiply(args[0], args[1])),
                new OperandDefinition(
                    "NUMBER",
                    @"\d*\.?\d+?",
                    x => Expression.Constant(double.Parse(x))),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );

            var expression = language.Parse<double>("1 + 2 * 3 + 5");
            Assert.AreEqual(12, expression.Compile()());
        }

        [Test]
        public void Should_apply_brackets()
        {
            BracketOpenDefinition openBracket;
            var language = new Language(
                new OperatorDefinition(
                    "PLUS",
                    @"\+",
                    1,
                    new[] { RelativePosition.Left, RelativePosition.Right },
                    args => Expression.Add(args[0], args[1])),
                new OperatorDefinition(
                    "MULTIPLY",
                    @"\*",
                    2,
                    new[] { RelativePosition.Left, RelativePosition.Right },
                    args => Expression.Multiply(args[0], args[1])),
                openBracket = new(
                    "OPENBRACKET",
                    @"\("),
                new BracketCloseDefinition(
                    "CLOSEBRACKET",
                    @"\)",
                    openBracket),
                new OperandDefinition(
                    "NUMBER",
                    @"\d*\.?\d+?",
                    x => Expression.Constant(double.Parse(x))),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );

            var expression = language.Parse<double>("(1 + 2) * (3 + 5)");
            Assert.AreEqual(24, expression.Compile()());
        }

        [Test]
        public void Should_run_single_param_functions()
        {
            BracketOpenDefinition openBracket, sinFn;
            var language = new Language(
                new OperatorDefinition(
                    "PLUS",
                    @"\+",
                    10,
                    new[] { RelativePosition.Left, RelativePosition.Right },
                    args => Expression.Add(args[0], args[1])),
                sinFn = new FunctionCallDefinition(
                    "SIN",
                    @"sin\(",
                    args => Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin))!, args[0])),
                openBracket = new(
                    "OPENBRACKET",
                    @"\("),
                new BracketCloseDefinition(
                    "CLOSEBRACKET",
                    @"\)",
                    new[] { openBracket, sinFn }),
                new OperandDefinition(
                    "NUMBER",
                    @"\d*\.?\d+?",
                    x => Expression.Constant(double.Parse(x))),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );

            var expression = language.Parse<double>("sin(1+2)+3");
            Assert.AreEqual(3.14, expression.Compile()(), 2);
        }

        [Test]
        public void Should_run_two_param_functions()
        {
            BracketOpenDefinition openBracket, logFn;
            GrammarDefinition listDelimeter;
            var language = new Language(
                new OperatorDefinition(
                    "PLUS",
                    @"\+",
                    1,
                    new[] { RelativePosition.Left, RelativePosition.Right },
                    args => Expression.Add(args[0], args[1])),
                logFn = new FunctionCallDefinition(
                    "LOG",
                    @"[Ll]og\(",
                    args => Expression.Call(ReflectionUtil<int>.Method(x => Math.Log(0, 0)), args)),
                openBracket = new(
                    "OPENBRACKET",
                    @"\("),
                listDelimeter = new ListDelimiterDefinition(
                    "COMMA",
                    @"\,"),
                new BracketCloseDefinition(
                    "CLOSEBRACKET",
                    @"\)",
                    new[] { openBracket, logFn },
                    listDelimeter),
                new OperandDefinition(
                    "NUMBER",
                    @"\d*\.?\d+?",
                    x => Expression.Constant(double.Parse(x))),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );

            var expression = language.Parse<double>("Log(1024,2) + 5");
            Assert.AreEqual(15, expression.Compile()());
        }

        [Test]
        public void Should_cast_function_params()
        {
            BracketOpenDefinition openBracket, logFn;
            GrammarDefinition listDelimeter;
            var language = new Language(
                logFn = new FunctionCallDefinition(
                    "LOG",
                    @"[Ll]og\(",
                    new[] { typeof(double), typeof(double) },
                    args => Expression.Call(ReflectionUtil<int>.Method(x => Math.Log(0, 0)), args)),
                openBracket = new(
                    "OPENBRACKET",
                    @"\("),
                listDelimeter = new ListDelimiterDefinition(
                    "COMMA",
                    @"\,"),
                new BracketCloseDefinition(
                    "CLOSEBRACKET",
                    @"\)",
                    new[] { openBracket, logFn },
                    listDelimeter),
                new OperandDefinition(
                    "NUMBER",
                    @"\d+",
                    x => Expression.Constant(int.Parse(x))),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );

            var expression = language.Parse<double>("Log(1024,2)");
            Assert.AreEqual(10, expression.Compile()());
        }
    }
}
