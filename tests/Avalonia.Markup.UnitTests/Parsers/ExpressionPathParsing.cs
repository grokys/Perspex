using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.ExpressionNodes.Reflection;
using Avalonia.Markup.Parsers;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class ExpressionObserverBuilderTests
    {
        private static readonly Func<string, string, Type> _typeResolver;

        static ExpressionObserverBuilderTests()
        {
            _typeResolver = (_, name) => name == "Owner" ? typeof(Owner) : null;
        }

        [Fact]
        public void Should_Build_Single_Property()
        {
            var result = Parse("Foo");

            AssertIsProperty(result[0], "Foo");
        }

        [Fact]
        public void Should_Build_Underscored_Property()
        {
            var result = Parse("_Foo");

            AssertIsProperty(result[0], "_Foo");
        }

        [Fact]
        public void Should_Build_Property_With_Digits()
        {
            var result = Parse("F0o");

            AssertIsProperty(result[0], "F0o");
        }

        [Fact]
        public void Should_Build_Dot()
        {
            var result = Parse(".");

            Assert.Null(result);
        }

        [Fact]
        public void Should_Build_Property_Chain()
        {
            var result = Parse("Foo.Bar.Baz");

            Assert.Equal(3, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsProperty(result[1], "Bar");
            AssertIsProperty(result[2], "Baz");
        }

        [Fact]
        public void Should_Build_Property_Chain_With_Null_Conditional()
        {
            var result = Parse("Foo.Bar?.Baz");

            Assert.Equal(3, result.Count);
            AssertIsProperty(result[0], "Foo", canBeNull: false);
            AssertIsProperty(result[1], "Bar", canBeNull: true);
            AssertIsProperty(result[2], "Baz", canBeNull: false);
        }

        [Fact]
        public void Should_Build_Property_Chain_With_Attached_Property_Null_Conditional()
        {
            var result = Parse("Foo.(Owner.Foo)?.Baz");

            Assert.Equal(3, result.Count);
            AssertIsProperty(result[0], "Foo", canBeNull: false);

            var attachedNode = Assert.IsType<AvaloniaPropertyAccessorNode>(result[1]);
            Assert.Equal(Owner.FooProperty, attachedNode.Property);
            Assert.True(attachedNode.CanBeNull);

            AssertIsProperty(result[2], "Baz", canBeNull: false);
        }

        [Fact]
        public void Should_Build_Negated_Property_Chain()
        {
            var result = Parse("!Foo.Bar.Baz");

            Assert.Equal(4, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsProperty(result[1], "Bar");
            AssertIsProperty(result[2], "Baz");
            Assert.IsType<LogicalNotNode>(result[3]);
        }

        [Fact]
        public void Should_Build_Double_Negated_Property_Chain()
        {
            var result = Parse("!!Foo.Bar.Baz");

            Assert.Equal(5, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsProperty(result[1], "Bar");
            AssertIsProperty(result[2], "Baz");
            Assert.IsType<LogicalNotNode>(result[3]);
            Assert.IsType<LogicalNotNode>(result[4]);
        }

        [Fact]
        public void Should_Build_Indexed_Property()
        {
            var result = Parse("Foo[15]");

            Assert.Equal(2, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsIndexer(result[1], "15");
            Assert.IsType<ReflectionIndexerNode>(result[1]);
        }

        [Fact]
        public void Should_Build_Indexed_Property_StringIndex()
        {
            var result = Parse("Foo[Key]");

            Assert.Equal(2, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsIndexer(result[1], "Key");
            Assert.IsType<ReflectionIndexerNode>(result[1]);
        }

        [Fact]
        public void Should_Build_Multiple_Indexed_Property()
        {
            var result = Parse("Foo[15,6]");

            Assert.Equal(2, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsIndexer(result[1], "15", "6");
        }

        [Fact]
        public void Should_Build_Multiple_Indexed_Property_With_Space()
        {
            var result = Parse("Foo[5, 16]");

            Assert.Equal(2, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsIndexer(result[1], "5", "16");
        }

        [Fact]
        public void Should_Build_Consecutive_Indexers()
        {
            var result = Parse("Foo[15][16]");

            Assert.Equal(3, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsIndexer(result[1], "15");
            AssertIsIndexer(result[2], "16");
        }

        [Fact]
        public void Should_Build_Indexed_Property_In_Chain()
        {
            var result = Parse("Foo.Bar[5, 6].Baz");

            Assert.Equal(4, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsProperty(result[1], "Bar");
            AssertIsIndexer(result[2], "5", "6");
            AssertIsProperty(result[3], "Baz");
        }

        [Fact]
        public void Should_Build_Stream_Node()
        {
            var result = Parse("Foo^");

            Assert.Equal(2, result.Count);
            Assert.IsType<DynamicPluginStreamNode>(result[1]);
        }

        private static void AssertIsProperty(ExpressionNode node, string name, bool canBeNull = false)
        {
            var p = Assert.IsType<DynamicPluginPropertyAccessorNode>(node);
            Assert.Equal(name, p.PropertyName);
            Assert.Equal(canBeNull, p.CanBeNull);
        }

        private static void AssertIsIndexer(ExpressionNode node, params string[] args)
        {
            var e = Assert.IsType<ReflectionIndexerNode>(node);
            Assert.Equal(e.Arguments.Cast<string>().ToArray(), args);
        }

        private static List<ExpressionNode> Parse(string path)
        {
            var reader = new CharacterReader(path.AsSpan());
            var (astNodes, sourceMode) = BindingExpressionGrammar.Parse(ref reader);
            return ExpressionNodeFactory.CreateFromAst(astNodes, _typeResolver, null, out _);
        }

        private static class Owner
        {
            public static readonly AttachedProperty<string> FooProperty =
                AvaloniaProperty.RegisterAttached<AvaloniaObject, string>(
                    "Foo",
                    typeof(Owner),
                    defaultValue: "foo");

            public static readonly AttachedProperty<AvaloniaObject> SomethingProperty =
                AvaloniaProperty.RegisterAttached<AvaloniaObject, AvaloniaObject>(
                    "Something",
                    typeof(Owner));
        }
    }
}
