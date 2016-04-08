﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Math;
using Loyc.Utilities;
using S = Loyc.Syntax.CodeSymbols;

/// <summary>Defines prelude macros, which are predefined macros that normally 
/// do not have to be explicitly imported before use (in LES or EC#).</summary>
namespace LeMP.Prelude
{
	/// <summary>Defines <c>noMacro(...)</c> for suppressing macro expansion and 
	/// <c>import macros your.namespace.name</c> as an alias for 
	/// <c>#importMacros(your.namespace.name)</c>.
	/// </summary>
	[ContainsMacros]
	public static partial class BuiltinMacros
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);

		[LexicalMacro("noMacro(Code)", "Pass code through to the output language, without macro processing.",
			Mode = MacroMode.NoReprocessing)]
		public static LNode noMacro(LNode node, IMacroContext sink)
		{
			if (!node.IsCall)
				return null;
			return node.WithTarget(S.Splice);
		}

		static readonly Symbol _hash_set = (Symbol)"#set";
		static readonly Symbol _hash_snippet = (Symbol)"#snippet";
		static readonly Symbol _hash_setScopedProperty = (Symbol)"#setScopedProperty";
		static readonly Symbol _hash_setScopedPropertyQuote = (Symbol)"#setScopedPropertyQuote";

		[LexicalMacro("#set Identifier = literal; #snippet Identifier = { statements; }; #snippet Identifier = expression;",
			"Sets an option, or saves a snippet of code for use later. See also: #get", 
			"#var", Mode = MacroMode.Passive)]
		public static LNode _set(LNode node, IMacroContext context)
		{
			var lhs = node.Args[0, LNode.Missing];
			var name = lhs.Name;
			bool isSnippet = name == _hash_snippet;
			if ((isSnippet || name == _hash_set) && node.ArgCount == 2 && lhs.IsId)
			{
				node = context.PreProcessChildren();

				Symbol newTarget = isSnippet ? _hash_setScopedPropertyQuote : _hash_setScopedProperty;
				var stmts = node.Args.Slice(1).Select(key =>
					{
						LNode value = F.@true;
						if (key.Calls(S.Assign, 2))
						{
							value = key.Args[1];
							key = key.Args[0];
							if (isSnippet && value.Calls(S.Braces))
								value = value.Args.AsLNode(S.Splice);
						}
						if (!key.IsId)
							context.Write(Severity.Error, key, "Invalid key; expected an identifier.");
						return node.With(newTarget, LNode.Literal(key.Name, key), value);
					});
				return F.Call(S.Splice, stmts);
			}
			return null;
		}

		[LexicalMacro("#get(key, defaultValueOpt)", 
			"Alias for #getScopedProperty. Gets a literal or code snippet that was previously set in this scope. "
			+"If the key is an identifier, it is treated as a symbol instead, e.g. `#get(Foo)` is equivalent to `#get(@@Foo)`.", 
			"#get")]
		public static LNode _get(LNode node, IMacroContext context)
		{
			if (node.ArgCount.IsInRange(1, 2))
				return MacroProcessorTask.getScopedProperty(node, context);
			return null;
		}

		static readonly Symbol _macros = GSymbol.Get("macros");
		static readonly Symbol _importMacros = GSymbol.Get("#importMacros");

		[LexicalMacro("import_macros Namespace",
			"Use macros from specified namespace. The 'macros' modifier imports macros only, deleting this statement from the output.")]
		public static LNode import_macros(LNode node, IMacroContext sink)
		{
			return node.With(_importMacros, node.Args);
		}

		[LexicalMacro("#printKnownMacros;", "Prints a table of all macros known to LeMP, as (invalid) C# code.",
			"printKnownMacros", "#printKnownMacros", Mode = MacroMode.NoReprocessing)]
		public static LNode printKnownMacros(LNode node, IMacroContext context)
		{
			// namespace LeMP {
			//     /* documentation */
			//     #fn;
			//     ...
			// }
			return F.Call(S.Splice, context.AllKnownMacros.SelectMany(p => p.Value)
				.GroupBy(mi => mi.NamespaceSym).OrderBy(g => g.Key).Select(group =>
					F.Attr(F.Trivia(S.TriviaSLCommentBefore, "printKnownMacros"),
					F.Call(S.Namespace, NamespaceSymbolToLNode(group.Key ?? GSymbol.Empty), LNode.Missing,
						F.Braces(group.OrderBy(mi => mi.Macro.Method.Name).Select(mi =>
						{
							StringBuilder descr = new StringBuilder(string.Format("\n\t\t### {0} ###\n",
								ParsingService.Current.Print(LNode.Id(mi.Name), null, ParsingService.Exprs)));
							if (!string.IsNullOrWhiteSpace(mi.Info.Syntax))
								descr.Append("\n\t\t\t").Append(mi.Info.Syntax.Replace("\n", "\n\t\t")).Append("\n");
							if (!string.IsNullOrWhiteSpace(mi.Info.Description))
								descr.Append("\n\t\t").Append(mi.Info.Description.Replace("\n", "\n\t\t")).Append("\n");
							descr.Append("\t");
							LNode line = LNode.Id(mi.Name ?? (Symbol)"<null>");

							string methodName = mi.Macro.Method.Name, @class = mi.Macro.Method.DeclaringType.Name;
							string postComment = " " + @class + "." + methodName;
							if (mi.Mode != MacroMode.Normal)
								postComment += string.Format(" (Mode = {0})", mi.Mode);
							return F.Attr(
								F.Trivia(S.TriviaMLCommentBefore, descr.ToString()),
								F.Trivia(S.TriviaSpaceBefore, "\n"), 
								F.Trivia(S.TriviaSLCommentAfter, postComment),
								line);
						}))))));
		}
		internal static LNode NamespaceSymbolToLNode(Symbol ns)
		{
			var parts = ns.Name.Split('.');
			return parts.Length == 1 ? F.Id(parts[0]) : F.Dot(parts);
		}
	}
}
