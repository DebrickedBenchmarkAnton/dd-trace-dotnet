//------------------------------------------------------------------------------
// <auto-generated />
// This file is part of YamlDotNet - A .NET library for YAML.
// Copyright (c) Antoine Aubry and contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using YamlDotNet.Deserializer.Core;
using YamlDotNet.Deserializer.Core.Events;
using YamlDotNet.Deserializer.Serialization;
using static YamlDotNet.Deserializer.Core.HashCode;

namespace YamlDotNet.Deserializer.RepresentationModel
{
    /// <summary>
    /// Represents a scalar node in the YAML document.
    /// </summary>
    [DebuggerDisplay("{Value}")]
    internal sealed class YamlScalarNode : YamlNode, IYamlConvertible
    {
        /// <summary>
        /// Gets or sets the value of the node.
        /// </summary>
        /// <value>The value.</value>
        internal string Value { get; set; }

        /// <summary>
        /// Gets or sets the style of the node.
        /// </summary>
        /// <value>The style.</value>
        internal ScalarStyle Style { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlScalarNode"/> class.
        /// </summary>
        internal YamlScalarNode(IParser parser, DocumentLoadingState state)
        {
            Load(parser, state);
        }

        private void Load(IParser parser, DocumentLoadingState state)
        {
            var scalar = parser.Consume<Scalar>();
            Load(scalar, state);
            Value = scalar.Value;
            Style = scalar.Style;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlScalarNode"/> class.
        /// </summary>
        internal YamlScalarNode()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlScalarNode"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        internal YamlScalarNode(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Resolves the aliases that could not be resolved when the node was created.
        /// </summary>
        /// <param name="state">The state of the document.</param>
        internal override void ResolveAliases(DocumentLoadingState state)
        {
            throw new NotSupportedException("Resolving an alias on a scalar node does not make sense");
        }

        /// <summary>
        /// Accepts the specified visitor by calling the appropriate Visit method on it.
        /// </summary>
        /// <param name="visitor">
        /// A <see cref="IYamlVisitor"/>.
        /// </param>
        internal override void Accept(IYamlVisitor visitor) => visitor.Visit(this);

        /// <summary />
        public override bool Equals(object obj) => obj is YamlScalarNode other
                && Equals(Tag, other.Tag)
                && Equals(Value, other.Value);

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode() => CombineHashCodes(Tag, Value);

        /// <summary>
        /// Performs an explicit conversion from <see cref="YamlScalarNode"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator string(YamlScalarNode value) => value.Value;

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        internal override string ToString(RecursionLevel level) => Value ?? string.Empty;

        /// <summary>
        /// Recursively enumerates all the nodes from the document, starting on the current node,
        /// and throwing <see cref="MaximumRecursionLevelReachedException"/>
        /// if <see cref="RecursionLevel.Maximum"/> is reached.
        /// </summary>
        internal override IEnumerable<YamlNode> SafeAllNodes(RecursionLevel level)
        {
            yield return this;
        }

        /// <summary>
        /// Gets the type of node.
        /// </summary>
        internal override YamlNodeType NodeType => YamlNodeType.Scalar;

        void IYamlConvertible.Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer) => Load(parser, new DocumentLoadingState());

    }
}