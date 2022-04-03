using LinqToQuerystring;
using StringToExpression.Exceptions;
using StringToExpression.LanguageDefinitions;
using StringToExpression.Test.Fixtures;
using StringToExpression.GrammarDefinitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;

namespace StringToExpression.Test
{
    public class LinqToQueryStringEquivalenceTests : IClassFixture<LinqToQuerystringTestDataFixture>
    {
        public readonly LinqToQuerystringTestDataFixture Data;

        public readonly ITestOutputHelper Output;

        public LinqToQueryStringEquivalenceTests(LinqToQuerystringTestDataFixture data, ITestOutputHelper output)
        {
            this.Data = data;
            this.Output = output;
        }

        [Theory]
        [TestCase("Complete")]
        [TestCase("not Complete")]
        [TestCase("Name eq 'Apple'")]
        [TestCase("'Apple' eq Name")]
        [TestCase("not Name eq 'Apple'")]
        [TestCase("Name ne 'Apple'")]
        [TestCase("not Name ne 'Apple'")]
        [TestCase("Age eq 4")]
        [TestCase("Age gt -4")]
        [TestCase("not Age eq 4")]
        [TestCase("Age ne 4")]
        [TestCase("not Age ne 4")]
        [TestCase("Age gt 3")]
        [TestCase("not Age gt 3")]
        [TestCase("Age ge 3")]
        [TestCase("not Age ge 3")]
        [TestCase("Age lt 3")]
        [TestCase("not Age lt 3")]
        [TestCase("Age le 3")]
        [TestCase("not Age le 3")]
        [TestCase("Population eq 40000000000L")]
        [TestCase("Population gt -40000000000L")]
        [TestCase("not Population eq 40000000000L")]
        [TestCase("Population ne 40000000000L")]
        [TestCase("not Population ne 40000000000L")]
        [TestCase("Population gt 30000000000L")]
        [TestCase("not Population gt 30000000000L")]
        [TestCase("Population ge 30000000000L")]
        [TestCase("not Population ge 30000000000L")]
        [TestCase("Population lt 30000000000L")]
        [TestCase("not Population lt 30000000000L")]
        [TestCase("Population le 30000000000L")]
        [TestCase("not Population le 30000000000L")]
        [TestCase("Code eq 34")]
        [TestCase("Code eq 0x22")]
        [TestCase("not Code eq 0x22")]
        [TestCase("Code ne 0x22")]
        [TestCase("not Code ne 0x22")]
        [TestCase("Code gt 0xCC")]
        [TestCase("not Code gt 0xCC")]
        [TestCase("Code ge 0xCC")]
        [TestCase("not Code ge 0xCC")]
        [TestCase("Code lt 0xCC")]
        [TestCase("not Code lt 0xCC")]
        [TestCase("Code le 0xCC")]
        [TestCase("not Code le 0xCC")]
        [TestCase("Guid eq guid'" + LinqToQuerystringTestDataFixture.guid1 + "'")]
        [TestCase("not Guid eq guid'" + LinqToQuerystringTestDataFixture.guid1 + "'")]
        [TestCase("Guid ne guid'" + LinqToQuerystringTestDataFixture.guid1 + "'")]
        [TestCase("not Guid ne guid'" + LinqToQuerystringTestDataFixture.guid1 + "'")]
        [TestCase("Cost eq 444.444f")]
        [TestCase("Cost gt -444.444f")]
        [TestCase("not Cost eq 444.444f")]
        [TestCase("Cost ne 444.444f")]
        [TestCase("not Cost ne 444.444f")]
        [TestCase("Cost gt 333.333f")]
        [TestCase("not Cost gt 333.333f")]
        [TestCase("Cost ge 333.333f")]
        [TestCase("not Cost ge 333.333f")]
        [TestCase("Cost lt 333.333f")]
        [TestCase("not Cost lt 333.333f")]
        [TestCase("Cost le 333.333f")]
        [TestCase("not Cost le 333.333f")]
        [TestCase("Value eq 444.444")]
        [TestCase("Cost gt -444.444")]
        [TestCase("not Value eq 444.444")]
        [TestCase("Value ne 444.444")]
        [TestCase("not Value ne 444.444")]
        [TestCase("Value gt 333.333")]
        [TestCase("not Value gt 333.333")]
        [TestCase("Value ge 333.333")]
        [TestCase("Value ge 333d")]
        [TestCase("not Value ge 333.333")]
        [TestCase("Value lt 333.333")]
        [TestCase("not Value lt 333.333")]
        [TestCase("Value le 333.333")]
        [TestCase("not Value le 333.333")]
        [TestCase("Score eq 0.4m")]
        [TestCase("Score eq 0.4")]
        [TestCase("Score eq 0.4m and Value eq 444.444")]
        [TestCase("Score gt -0.4m")]
        [TestCase("not Score eq 0.4m")]
        [TestCase("Score ne 0.4m")]
        [TestCase("not Score ne 0.4m")]
        [TestCase("Score gt 0.3m")]
        [TestCase("not Score gt 0.3m")]
        [TestCase("Score ge 0.3m")]
        [TestCase("not Score ge 0.3m")]
        [TestCase("Score lt 0.3m")]
        [TestCase("not Score lt 0.3m")]
        [TestCase("Score le 0.3m")]
        [TestCase("not Score le 0.3m")]
        [TestCase("color eq 1")]
        [TestCase("Date eq datetime'2002-01-01T00:00'")]
        [TestCase("not Date eq datetime'2002-01-01T00:00'")]
        [TestCase("Date ne datetime'2002-01-01T00:00'")]
        [TestCase("not Date ne datetime'2002-01-01T00:00'")]
        [TestCase("Date gt datetime'2003-01-01T00:00'")]
        [TestCase("not Date gt datetime'2003-01-01T00:00'")]
        [TestCase("Date ge datetime'2003-01-01T00:00'")]
        [TestCase("not Date ge datetime'2003-01-01T00:00'")]
        [TestCase("Date lt datetime'2003-01-01T00:00'")]
        [TestCase("not Date lt datetime'2003-01-01T00:00'")]
        [TestCase("Date le datetime'2003-01-01T00:00'")]
        [TestCase("not Date le datetime'2003-01-01T00:00'")]
        [TestCase("Complete eq true")]
        [TestCase("not Complete eq true")]
        [TestCase("Complete ne true")]
        [TestCase("not Complete ne true")]
        [TestCase("Name eq 'Custard' and Age ge 2")]
        [TestCase("Name eq 'Custard' and not Age lt 2")]
        [TestCase("Name eq 'Banana' or Date gt datetime'2003-01-01T00:00'")]
        [TestCase("Name eq 'Banana' or not Date le datetime'2003-01-01T00:00'")]
        [TestCase("Name eq 'Apple' and Complete eq true or Date gt datetime'2003-01-01T00:00'")]
        [TestCase("Name eq 'Apple' and Complete eq true or not Date le datetime'2003-01-01T00:00'")]
        [TestCase("Name eq 'Apple' and (Complete eq true or Date gt datetime'2003-01-01T00:00')")]
        [TestCase("not (Name eq 'Apple' and (Complete eq true or Date gt datetime'2003-01-01T00:00'))")]
        public void When_concrete_data_should_return_same_results_as_linqToQuerystring(string query)
        {
            var linqToQuerystringFiltered = Data.ConcreteCollection.LinqToQuerystring("?$filter=" + query).ToList();

            var filter = new ODataFilterLanguage().Parse<LinqToQuerystringTestDataFixture.ConcreteClass>(query);
            var stringParserFiltered = Data.ConcreteCollection.Where(filter).ToList();

            Assert.AreEqual(linqToQuerystringFiltered, stringParserFiltered);
        }

        [Theory]
        [TestCase(@"Name eq 'Apple\\Bob'")]
        [TestCase(@"Name eq 'Apple\bBob'")]
        [TestCase(@"Name eq 'Apple\tBob'")]
        [TestCase(@"Name eq 'Apple\nBob'")]
        [TestCase(@"Name eq 'Apple\fBob'")]
        [TestCase(@"Name eq 'Apple\rBob'")]
        [TestCase(@"Name eq 'Apple""Bob'")]
        [TestCase(@"Name eq 'Apple\'Bob'")]
       
        public void When_edgecase_data_should_return_same_results_as_linqToQuerystring(string query)
        {
            var linqToQuerystringFiltered = Data.EdgeCaseCollection.LinqToQuerystring("?$filter=" + query).ToList();

            var filter = new ODataFilterLanguage().Parse<LinqToQuerystringTestDataFixture.ConcreteClass>(query);
            var stringParserFiltered = Data.EdgeCaseCollection.Where(filter).ToList();

            Assert.AreEqual(linqToQuerystringFiltered, stringParserFiltered);
        }

        [Theory]
        [TestCase("Age eq 1")]
        [TestCase("1 eq Age")]
        [TestCase("Age ne 1")]
        [TestCase("1 ne Age")]
        [TestCase("Age gt 0")]
        [TestCase("2 gt Age")]
        [TestCase("Age ge 1")]
        [TestCase("1 ge Age")]
        [TestCase("Age lt 2")]
        [TestCase("0 lt Age")]
        [TestCase("Age le 1")]
        [TestCase("1 le Age")]
        [TestCase("Age eq null")]
        [TestCase("null eq Age")]
        [TestCase("Age ne null")]
        [TestCase("null ne Age")]
        [TestCase("Age gt null")]
        [TestCase("null gt Age")]
        [TestCase("Age ge null")]
        [TestCase("null ge Age")]
        [TestCase("Age lt null")]
        [TestCase("null lt Age")]
        [TestCase("Age le null")]
        [TestCase("null le Age")]
        [TestCase("Date eq datetime'2002-01-01T00:00'")]
        [TestCase("Complete eq true")]
        [TestCase("Complete")]
        [TestCase("Complete eq false")]
        [TestCase("not Complete eq true")]
        [TestCase("not Complete")]
        [TestCase("not Complete eq false")]
        [TestCase("Population eq 10000000000L")]
        [TestCase("Value eq 111.111")]
        [TestCase("Cost eq 111.111f")]
        [TestCase("Code eq 0x00")]
        [TestCase("Guid eq guid'" + LinqToQuerystringTestDataFixture.guid0 + "'")]
        [TestCase("Name eq null")]
        [TestCase("null eq Name")]
        [TestCase("Name ne null")]
        [TestCase("null ne Name")]
        public void When_nullable_data_should_return_same_results_as_linqToQuerystring(string query)
         {
            var linqToQuerystringFiltered = Data.NullableCollection.LinqToQuerystring("?$filter=" + query).ToList();

            var filter = new ODataFilterLanguage().Parse<LinqToQuerystringTestDataFixture.NullableClass>(query);
            var stringParserFiltered = Data.NullableCollection.Where(filter).ToList();

            Assert.AreEqual(linqToQuerystringFiltered, stringParserFiltered);
        }

        [Theory]
        [TestCase("startswith(Name,'Sat')")]
        [TestCase("endswith(Name,'day')")]
        [TestCase("substringof('urn',Name)")]
        [TestCase("(substringof('Mond',Name)) or (substringof('Tues',Name))")]
        [TestCase(@"substringof('sat',tolower(Name))")]
        [TestCase(@"substringof('SAT',toupper(Name))")]
        [TestCase(@"year(Date) eq 2005")]
        [TestCase(@"month(Date) eq 6")]
        [TestCase(@"day(Date) eq 2")]
        [TestCase(@"hour(Date) eq 10")]
        [TestCase(@"minute(Date) eq 20")]
        [TestCase(@"second(Date) eq 50")]
        public void When_functions_return_same_results_as_linqToQuerystring(string query)
        {
            var linqToQuerystringFiltered = Data.FunctionConcreteCollection.LinqToQuerystring("?$filter=" + query).ToList();

            var filter = new ODataFilterLanguage().Parse<LinqToQuerystringTestDataFixture.ConcreteClass>(query);
            var stringParserFiltered = Data.FunctionConcreteCollection.Where(filter).ToList();

            Assert.AreEqual(linqToQuerystringFiltered, stringParserFiltered);
        }


        [Theory]
        [TestCase("concrete/complete")]
        [TestCase("substringof('a',concrete/name)")]
        public void When_property_paths_results_as_linqToQuerystring(string query)
        {
            var linqToQuerystringFiltered = Data.ComplexCollection.LinqToQuerystring("?$filter=" + query).ToList();

            var filter = new ODataFilterLanguage().Parse<LinqToQuerystringTestDataFixture.ComplexClass>(query);
            var stringParserFiltered = Data.ComplexCollection.Where(filter).ToList();

            Assert.AreEqual(linqToQuerystringFiltered, stringParserFiltered);
        }

        [Theory]
        [TestCase(@"Id eq null")]
        [TestCase(@"Id eq 'somestring'")]
        [TestCase(@"Id eq Name")]
        public void When_invalid_checks_should_error(string query)
        {
            var linqToQuerystringException = Assert.ThrowsAny<Exception>(()=>Data.ConcreteCollection.LinqToQuerystring("?$filter=" + query).ToList());
            var stringToExprssionException = Assert.Throws<OperationInvalidException>(() => new ODataFilterLanguage().Parse<LinqToQuerystringTestDataFixture.ConcreteClass>(query));
        }


        [Fact(Skip ="Performance sanity check.")]
        public void Should_be_faster_than_linqToQuerystring()
        {
            var baseDatetime = new DateTime(2003, 01, 01);

            var linqToQueryStringStopwatch = new Stopwatch();
            linqToQueryStringStopwatch.Start();
            for(int i = 0; i < 10000; i++)
            {
                var date = baseDatetime.AddDays(i).ToString("s");
                var linqToQuerystringFiltered = Data.ConcreteCollection.LinqToQuerystring($"?$filter=Name eq 'Apple' and (Complete eq true or Date gt datetime'{date}')");
            }
            linqToQueryStringStopwatch.Stop();


           
            var parseStringStopwatch = new Stopwatch();
            parseStringStopwatch.Start();
            var language = new ODataFilterLanguage();
            for (int i = 0; i < 10000; i++)
            {
                var date = baseDatetime.AddDays(i).ToString("s");
                var filter = language.Parse<LinqToQuerystringTestDataFixture.ConcreteClass>($"Name eq 'Apple' and (Complete eq true or Date gt datetime'{date}')");
            }
            parseStringStopwatch.Stop();

            Output.WriteLine($"LinqToQueryString Duration: {linqToQueryStringStopwatch.Elapsed}");
            Output.WriteLine($"StringToExpression Duration: {parseStringStopwatch.Elapsed}");

            Assert.True(parseStringStopwatch.ElapsedMilliseconds < linqToQueryStringStopwatch.ElapsedMilliseconds);
        }

    }
}
