//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
#pragma warning disable CS0618, CS0649, CS1574, CS1580, CS1581, CS1584, CS1591, CS1573, CS8018, SYSLIB0011, SYSLIB0032
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8620, CS8714, CS8762, CS8765, CS8766, CS8767, CS8768, CS8769, CS8612, CS8629, CS8774
#nullable enable
#if NETCOREAPP3_1_OR_GREATER
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Datadog.Trace.Vendors.IndieSystem.Text.RegularExpressions{
    using System;


    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal partial class SR {

        private static global::System.Resources.ResourceManager resourceMan;

        private static global::System.Globalization.CultureInfo resourceCulture;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SR() {
        }

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Datadog.Trace.Vendors.IndieSystem.Text.RegularExpressions.SR", typeof(SR).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Alternation conditions cannot be comments..
        /// </summary>
        internal static string AlternationHasComment {
            get {
                return ResourceManager.GetString("AlternationHasComment", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Illegal conditional (?(...)) expression..
        /// </summary>
        internal static string AlternationHasMalformedCondition {
            get {
                return ResourceManager.GetString("AlternationHasMalformedCondition", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Conditional alternation is missing a closing parenthesis after the group number {0}..
        /// </summary>
        internal static string AlternationHasMalformedReference {
            get {
                return ResourceManager.GetString("AlternationHasMalformedReference", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Alternation conditions do not capture and cannot be named..
        /// </summary>
        internal static string AlternationHasNamedCapture {
            get {
                return ResourceManager.GetString("AlternationHasNamedCapture", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Too many | in (?()|)..
        /// </summary>
        internal static string AlternationHasTooManyConditions {
            get {
                return ResourceManager.GetString("AlternationHasTooManyConditions", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Conditional alternation refers to an undefined group number {0}..
        /// </summary>
        internal static string AlternationHasUndefinedReference {
            get {
                return ResourceManager.GetString("AlternationHasUndefinedReference", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Destination array is not long enough to copy all the items in the collection. Check array index and length..
        /// </summary>
        internal static string Arg_ArrayPlusOffTooSmall {
            get {
                return ResourceManager.GetString("Arg_ArrayPlusOffTooSmall", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Start index cannot be less than 0 or greater than input length..
        /// </summary>
        internal static string BeginIndexNotNegative {
            get {
                return ResourceManager.GetString("BeginIndexNotNegative", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Invalid group name: Group names must begin with a word character..
        /// </summary>
        internal static string CaptureGroupNameInvalid {
            get {
                return ResourceManager.GetString("CaptureGroupNameInvalid", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Capture number cannot be zero..
        /// </summary>
        internal static string CaptureGroupOfZero {
            get {
                return ResourceManager.GetString("CaptureGroupOfZero", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Count cannot be less than -1..
        /// </summary>
        internal static string CountTooSmall {
            get {
                return ResourceManager.GetString("CountTooSmall", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Enumeration has either not started or has already finished..
        /// </summary>
        internal static string EnumNotStarted {
            get {
                return ResourceManager.GetString("EnumNotStarted", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to A subtraction must be the last element in a character class..
        /// </summary>
        internal static string ExclusionGroupNotLast {
            get {
                return ResourceManager.GetString("ExclusionGroupNotLast", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to atomic subexpressions (?&gt; pattern).
        /// </summary>
        internal static string ExpressionDescription_AtomicSubexpressions {
            get {
                return ResourceManager.GetString("ExpressionDescription_AtomicSubexpressions", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to backreference (\\ number).
        /// </summary>
        internal static string ExpressionDescription_Backreference {
            get {
                return ResourceManager.GetString("ExpressionDescription_Backreference", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to balancing group (?&lt;name1-name2&gt;subexpression) or (?&apos;name1-name2&apos; subexpression).
        /// </summary>
        internal static string ExpressionDescription_BalancingGroup {
            get {
                return ResourceManager.GetString("ExpressionDescription_BalancingGroup", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to captured group conditional (?( name ) yes-pattern | no-pattern ) or (?( number ) yes-pattern| no-pattern ).
        /// </summary>
        internal static string ExpressionDescription_Conditional {
            get {
                return ResourceManager.GetString("ExpressionDescription_Conditional", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to contiguous matches (\\G).
        /// </summary>
        internal static string ExpressionDescription_ContiguousMatches {
            get {
                return ResourceManager.GetString("ExpressionDescription_ContiguousMatches", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to test conditional (?( test-pattern ) yes-pattern | no-pattern ).
        /// </summary>
        internal static string ExpressionDescription_IfThenElse {
            get {
                return ResourceManager.GetString("ExpressionDescription_IfThenElse", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to negative lookahead (?! pattern) or negative lookbehind (?&lt;! pattern).
        /// </summary>
        internal static string ExpressionDescription_NegativeLookaround {
            get {
                return ResourceManager.GetString("ExpressionDescription_NegativeLookaround", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to positive lookahead (?= pattern) or positive lookbehind (?&lt;= pattern).
        /// </summary>
        internal static string ExpressionDescription_PositiveLookaround {
            get {
                return ResourceManager.GetString("ExpressionDescription_PositiveLookaround", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Regular expression parser error &apos;{0}&apos; at offset {1}..
        /// </summary>
        internal static string Generic {
            get {
                return ResourceManager.GetString("Generic", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to AppDomain data &apos;{0}&apos; contains the invalid value or object &apos;{1}&apos; for specifying a default matching timeout for System.Text.RegularExpressions.Regex..
        /// </summary>
        internal static string IllegalDefaultRegexMatchTimeoutInAppDomain {
            get {
                return ResourceManager.GetString("IllegalDefaultRegexMatchTimeoutInAppDomain", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Not enough )&apos;s..
        /// </summary>
        internal static string InsufficientClosingParentheses {
            get {
                return ResourceManager.GetString("InsufficientClosingParentheses", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Too many )&apos;s..
        /// </summary>
        internal static string InsufficientOpeningParentheses {
            get {
                return ResourceManager.GetString("InsufficientOpeningParentheses", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Insufficient hexadecimal digits..
        /// </summary>
        internal static string InsufficientOrInvalidHexDigits {
            get {
                return ResourceManager.GetString("InsufficientOrInvalidHexDigits", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Unrecognized grouping construct..
        /// </summary>
        internal static string InvalidGroupingConstruct {
            get {
                return ResourceManager.GetString("InvalidGroupingConstruct", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Incomplete \\p{X} character escape..
        /// </summary>
        internal static string InvalidUnicodePropertyEscape {
            get {
                return ResourceManager.GetString("InvalidUnicodePropertyEscape", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Length cannot be less than 0 or exceed input length..
        /// </summary>
        internal static string LengthNotNegative {
            get {
                return ResourceManager.GetString("LengthNotNegative", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Invalid pattern &apos;{0}&apos; at offset {1}. {2}.
        /// </summary>
        internal static string MakeException {
            get {
                return ResourceManager.GetString("MakeException", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Malformed \\k&lt;...&gt; named back reference..
        /// </summary>
        internal static string MalformedNamedReference {
            get {
                return ResourceManager.GetString("MalformedNamedReference", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Malformed \\p{X} character escape..
        /// </summary>
        internal static string MalformedUnicodePropertyEscape {
            get {
                return ResourceManager.GetString("MalformedUnicodePropertyEscape", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Missing control character..
        /// </summary>
        internal static string MissingControlCharacter {
            get {
                return ResourceManager.GetString("MissingControlCharacter", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Nested quantifier &apos;{0}&apos;..
        /// </summary>
        internal static string NestedQuantifiersNotParenthesized {
            get {
                return ResourceManager.GetString("NestedQuantifiersNotParenthesized", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Result cannot be called on a failed Match..
        /// </summary>
        internal static string NoResultOnFailed {
            get {
                return ResourceManager.GetString("NoResultOnFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Regex replacements with substitutions of groups are not supported with RegexOptions.NonBacktracking..
        /// </summary>
        internal static string NotSupported_NonBacktrackingAndReplacementsWithSubstitutionsOfGroups {
            get {
                return ResourceManager.GetString("NotSupported_NonBacktrackingAndReplacementsWithSubstitutionsOfGroups", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to RegexOptions.NonBacktracking is not supported in conjunction with expressions containing: &apos;{0}&apos;..
        /// </summary>
        internal static string NotSupported_NonBacktrackingConflictingExpression {
            get {
                return ResourceManager.GetString("NotSupported_NonBacktrackingConflictingExpression", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to The specified pattern with RegexOptions.NonBacktracking could result in an automata as large as &apos;{0}&apos; nodes, which is larger than the configured limit of &apos;{1}&apos;..
        /// </summary>
        internal static string NotSupported_NonBacktrackingUnsafeSize {
            get {
                return ResourceManager.GetString("NotSupported_NonBacktrackingUnsafeSize", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Collection is read-only..
        /// </summary>
        internal static string NotSupported_ReadOnlyCollection {
            get {
                return ResourceManager.GetString("NotSupported_ReadOnlyCollection", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to This platform does not support writing compiled regular expressions to an assembly. Use RegexGeneratorAttribute with the regular expression source generator instead..
        /// </summary>
        internal static string PlatformNotSupported_CompileToAssembly {
            get {
                return ResourceManager.GetString("PlatformNotSupported_CompileToAssembly", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Quantifier &apos;{0}&apos; following nothing..
        /// </summary>
        internal static string QuantifierAfterNothing {
            get {
                return ResourceManager.GetString("QuantifierAfterNothing", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Quantifier and capture group numbers must be less than or equal to Int32.MaxValue..
        /// </summary>
        internal static string QuantifierOrCaptureGroupOutOfRange {
            get {
                return ResourceManager.GetString("QuantifierOrCaptureGroupOutOfRange", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to The Regex engine has timed out while trying to match a pattern to an input string. This can occur for many reasons, including very large inputs or excessive backtracking caused by nested quantifiers, back-references and other factors..
        /// </summary>
        internal static string RegexMatchTimeoutException_Occurred {
            get {
                return ResourceManager.GetString("RegexMatchTimeoutException_Occurred", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to [x-y] range in reverse order..
        /// </summary>
        internal static string ReversedCharacterRange {
            get {
                return ResourceManager.GetString("ReversedCharacterRange", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Illegal {x,y} with x &gt; y..
        /// </summary>
        internal static string ReversedQuantifierRange {
            get {
                return ResourceManager.GetString("ReversedQuantifierRange", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Cannot include class \\{0} in character range..
        /// </summary>
        internal static string ShorthandClassInCharacterRange {
            get {
                return ResourceManager.GetString("ShorthandClassInCharacterRange", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Reference to undefined group name &apos;{0}&apos;..
        /// </summary>
        internal static string UndefinedNamedReference {
            get {
                return ResourceManager.GetString("UndefinedNamedReference", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Reference to undefined group number {0}..
        /// </summary>
        internal static string UndefinedNumberedReference {
            get {
                return ResourceManager.GetString("UndefinedNumberedReference", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Illegal \\ at end of pattern..
        /// </summary>
        internal static string UnescapedEndingBackslash {
            get {
                return ResourceManager.GetString("UnescapedEndingBackslash", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Unrecognized control character..
        /// </summary>
        internal static string UnrecognizedControlCharacter {
            get {
                return ResourceManager.GetString("UnrecognizedControlCharacter", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Unrecognized escape sequence \\{0}..
        /// </summary>
        internal static string UnrecognizedEscape {
            get {
                return ResourceManager.GetString("UnrecognizedEscape", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Unknown property &apos;{0}&apos;..
        /// </summary>
        internal static string UnrecognizedUnicodeProperty {
            get {
                return ResourceManager.GetString("UnrecognizedUnicodeProperty", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Unterminated [] set..
        /// </summary>
        internal static string UnterminatedBracket {
            get {
                return ResourceManager.GetString("UnterminatedBracket", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Unterminated (?#...) comment..
        /// </summary>
        internal static string UnterminatedComment {
            get {
                return ResourceManager.GetString("UnterminatedComment", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Searching an input span using a pre-compiled Regex assembly is not supported. Please use the string overloads or use a newer Regex implementation..
        /// </summary>
        internal static string UsingSpanAPIsWithCompiledToAssembly {
            get {
                return ResourceManager.GetString("UsingSpanAPIsWithCompiledToAssembly", resourceCulture);
            }
        }
    }
}

#endif