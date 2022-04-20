using StringToExpression.GrammarDefinitions;
using System.Linq;
using NUnit.Framework;

namespace StringToExpression.Test
{
    public class TokenizerTests
    {
        [Test]
        public void Should_tokenize_with_definition_and_value()
        {
            var language = new Language(
                new GrammarDefinition("PLUS", @"\+"),
                new GrammarDefinition("SUBTRACT", @"\-"),
                new GrammarDefinition("NUMBER", @"\d*\.?\d+?"),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );

            var tokens = language.Tokenizer.Tokenize("1 + 2 + 3 - 5").ToList();
            var tokenNames = tokens.Select(x => $"{x.Definition.Name} {x.Value}");

            Assert.AreEqual(new[]
            {
                "NUMBER 1",
                "PLUS +",
                "NUMBER 2",
                "PLUS +",
                "NUMBER 3",
                "SUBTRACT -",
                "NUMBER 5",
            }, tokenNames);
        }

        [Test]
        public void When_unknown_token_should_throw()
        {
            var language = new Language(
                new GrammarDefinition("PLUS", @"\+"),
                new GrammarDefinition("SUBTRACT", @"\-"),
                new GrammarDefinition("NUMBER", @"\d*\.?\d+?"),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );
            var exception = Assert.Throws<GrammarUnknownException>(() => language.Tokenizer.Tokenize("1 + 2 * 3 - 5").ToList());
            Assert.AreEqual("1 + 2 [*] 3 - 5", exception.UnexpectedGrammarSubstring.Highlight());
        }

        [Test]
        public void When_skip_should_not_return_token()
        {
            var language = new Language(
                new GrammarDefinition("A", @"A"),
                new GrammarDefinition("B", @"B", true)
            );

            var tokens = language.Tokenizer.Tokenize("AABBAA").ToList();
            Assert.AreEqual(new[]
            {
                "A", "A", "A", "A"
            }, tokens.Select(x => $"{x.Value}"));
        }

        [Test]
        public void When_regex_complicated_should_function_correctly()
        {
            var language = new Language(
                new GrammarDefinition("LITERAL", @"\'([a-zA-Z0-9]+)\'"),
                new GrammarDefinition("AND", @"[Aa][Nn][Dd]"),
                new GrammarDefinition("EQ", @"[Ee][Qq]"),
                new GrammarDefinition("NUMBER", @"\d*\.?\d+?"),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );

            var tokens = language.Tokenizer.Tokenize("1 EQ '1' and 2 eq '2' ").ToList();
            var tokenNames = tokens.Select(x => $"{x.Definition.Name} {x.Value}");

            Assert.AreEqual(new[]
            {
                "NUMBER 1",
                "EQ EQ",
                "LITERAL '1'",
                "AND and",
                "NUMBER 2",
                "EQ eq",
                "LITERAL '2'",
            }, tokenNames);
        }

        [Test]
        public void When_look_lookahead_should_capture_only_value()
        {
            var language = new Language(
                new GrammarDefinition("FUNCTION", @"[a-zA-Z]+(?=\()"),
                new GrammarDefinition("WORD", @"[a-zA-Z]+"),
                new GrammarDefinition("OPEN_BRACKET", @"\("),
                new GrammarDefinition("CLOSER_BRACKET", @"\)"),
                new GrammarDefinition("WHITESPACE", @"\s+", true)
            );

            var tokens = language.Tokenizer.Tokenize("I am some func()").ToList();
            var tokenNames = tokens.Select(x => $"{x.Definition.Name} {x.Value}");

            Assert.AreEqual(new[]
            {
                "WORD I",
                "WORD am",
                "WORD some",
                "FUNCTION func",
                "OPEN_BRACKET (",
                "CLOSER_BRACKET )",
            }, tokenNames);
        }

        [Test]
        public void Should_capture_based_on_order()
        {
            //AB has a higher capture order order than individual A's and B's
            var language1 = new Language(
                new GrammarDefinition("AB", "AB"),
                new GrammarDefinition("A", "A"),
                new GrammarDefinition("B", "B")
            );

            var tokensTypes1 = language1.Tokenizer.Tokenize("AABB")
                .Select(x => $"{x.Definition.Name}")
                .ToList();
            Assert.AreEqual(new[]
            {
                "A",
                "AB",
                "B",
            }, tokensTypes1);

            //Individual A's and B's characters have higher capture order than AB
            var language2 = new Language(
                new GrammarDefinition("A", "A"),
                new GrammarDefinition("B", "B"),
                new GrammarDefinition("AB", "AB")
            );

            var tokensTypes2 = language2.Tokenizer.Tokenize("AABB")
                .Select(x => $"{x.Definition.Name}")
                .ToList();
            Assert.AreEqual(new[]
            {
                "A",
                "A",
                "B",
                "B",
            }, tokensTypes2);
        }

        [Test]
        public void When_invalid_definition_name_should_throw()
        {
            var exception = Assert.Throws<GrammarDefinitionInvalidNameException>(() => new GrammarDefinition("I-am-not a valid name", @"B"));
            Assert.AreEqual("I-am-not a valid name", exception.GrammarDefinitionName);
        }
    }
}
