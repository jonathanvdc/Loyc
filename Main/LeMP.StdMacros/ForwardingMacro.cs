﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Ecs;

namespace LeMP
{
	using S = CodeSymbols;

	public partial class StandardMacros
	{
		static readonly Symbol _hash = GSymbol.Get("#");

		[LexicalMacro("Type SomeMethod(Type param) ==> target._;",
			"Forward a call to another method. The target method must not include an "+
			"argument list; the method parameters are forwarded automatically. If the "+
			"target expression includes an underscore (`_`), it is replaced with the "+
			"name of the current function. For example, `int Compute(int x) ==> base._` "+
			"is implemented as `int Compute(int x) { return base.Compute(x); }`", 
			"#fn", Mode = MacroMode.Passive | MacroMode.Normal)]
		public static LNode ForwardMethod(LNode fn, IMacroContext context)
		{
			LNode args, fwd, body;
			if (fn.ArgCount != 4 || !(fwd = fn.Args[3]).Calls(S.Forward, 1) || !(args = fn.Args[2]).Calls(S.AltList))
				return null;

			VList<LNode> argList = GetArgNamesFromFormalArgList(args, formalArg =>
				context.Write(Severity.Error, formalArg, "'==>': Expected a variable declaration here"));

			LNode target = GetForwardingTarget(fn.Args[1], fwd);
			LNode call = F.Call(target, argList);
			
			bool isVoidFn = fn.Args[0].IsIdNamed(S.Void);
			body = F.Braces(isVoidFn ? call : F.Call(S.Return, call));
			return fn.WithArgChanged(3, body);
		}

		internal static VList<LNode> GetArgNamesFromFormalArgList(LNode args, Action<LNode> onError)
		{
			VList<LNode> formalArgs = args.Args;
			VList<LNode> argList = VList<LNode>.Empty;
			foreach (var formalArg in formalArgs)
			{
				if (!formalArg.Calls(S.Var, 2)) {
					onError(formalArg);
				} else {
					LNode argName = formalArg.Args[1];
					if (argName.Calls(S.Assign, 2))
						argName = argName.Args[0];
					LNode @ref = formalArg.AttrNamed(S.Ref) ?? formalArg.AttrNamed(S.Out);
					if (@ref != null)
						argName = argName.PlusAttr(@ref);
					argList.Add(argName);
				}
			}
			return argList;
		}

		[LexicalMacro("Type Prop ==> target; Type Prop { get ==> target; set ==> target; }",
			"Forward property getter and/or setter. If the first syntax is used (with no braces), only the getter is forwarded.", 
			"#property", Mode = MacroMode.Passive | MacroMode.Normal)]
		public static LNode ForwardProperty(LNode prop, IMacroContext context)
		{
			LNode name, fwd, body;
			if (prop.ArgCount != 4)
				return null;
			LNode target = GetForwardingTarget(name = prop.Args[1], fwd = prop.Args[3]);
			if (target != null)
			{
				body = F.Braces(F.Call(S.get, F.Braces(F.Call(S.Return, target))).SetBaseStyle(NodeStyle.Special));
				return prop.WithArgChanged(3, body);
			}
			else if ((body = fwd).Calls(S.Braces))
			{
				var body2 = body.WithArgs(stmt => {
					if (stmt.Calls(S.get, 1) && (target = GetForwardingTarget(name, stmt.Args[0])) != null)
						return stmt.WithArgs(new VList<LNode>(F.Braces(F.Call(S.Return, target))));
					if (stmt.Calls(S.set, 1) && (target = GetForwardingTarget(name, stmt.Args[0])) != null)
						return stmt.WithArgs(new VList<LNode>(F.Braces(F.Call(S.Assign, target, F.Id(S.value)))));
					return stmt;
				});
				if (body2 != body)
					return prop.WithArgChanged(3, body2);
			}
			return null;
		}
		static LNode GetForwardingTarget(LNode methodName, LNode fwd)
		{
			if (fwd.Calls(S.Forward, 1)) {
				LNode target = fwd.Args[0];
				if (target.Calls(S.Dot, 2) && (target.Args[1].IsIdNamed(_hash) || target.Args[1].IsIdNamed(__)))
					return target.WithArgChanged(1, target.Args[1].WithName(
						EcsNodePrinter.KeyNameComponentOf(methodName)));
				return target;
			} else
				return null;
		}
	}
}
