using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Loyc.Math;
using Loyc.Collections.Impl;
using System.Diagnostics;
using Loyc.Collections;

namespace Loyc
{
	public static class TypeExt
	{
		public static string NameWithGenericArgs(this Type type)
		{
			string result = type.Name;
			if (type.IsGenericType)
			{
				// remove generic parameter count (e.g. `1)
				int i = result.LastIndexOf('`');
				if (i > 0)
					result = result.Substring(0, i);

				result = string.Format(
					"{0}<{1}>",
					result,
					StringExt.Join(", ", type.GetGenericArguments()
					                     .Select(t => NameWithGenericArgs(t))));
			}
			return result;
		}
	}

	public static class ExceptionExt
	{
		/// <summary>Returns a string of the form "{ex.Message} ({ex.GetType().Name})".</summary>
		public static string ExceptionMessageAndType(this Exception ex) {
			return string.Format("{0} ({1})", ex.Message, ex.GetType().Name);
		}
		/// <summary>Gets the innermost InnerException, or <c>ex</c> itself if there are no inner exceptions.</summary>
		/// <exception cref="NullReferenceException">ex is null.</exception>
		public static Exception InnermostException(this Exception ex)
		{
			while (ex.InnerException != null)
				ex = ex.InnerException;
			return ex;
		}
		/// <inheritdoc cref="Description(Exception, bool, string)"/>
		public static string Description(this Exception ex) { return Description(ex, false); }
		/// <inheritdoc cref="Description(Exception, bool, string)"/>
		/// <remarks>Adds a stack trace.</remarks>
		public static string DescriptionAndStackTrace(this Exception ex) { return Description(ex, true); }
		/// <summary>Gets a description of the exception in the form "{ex.Message} ({ex.GetType().Name})".
		/// If the exception has InnerExceptions, these are printed afterward in 
		/// the form "Inner exception: {ex.Message} ({ex.GetType().Name})" and 
		/// separated from the outer exception by "\n\n" (or a string of your 
		/// choosing).</summary>
		/// <param name="addStackTrace">If true, the stack trace of the outermost
		/// exception is added to the end of the message (not the innermost 
		/// exception, because the inner stack trace gets truncated. TODO: 
		/// investigate whether the full stack trace can be reconstructed).</param>
		/// <param name="lineSeparator">Separator between different exceptions and 
		/// before the stack trace.</param>
		public static string Description(this Exception ex, bool addStackTrace, string lineSeparator = "\n\n")
		{
			Exception inner = ex;
			StringBuilder msg = new StringBuilder();
			do {
				if (inner != ex) {
					msg.Append(lineSeparator);
					msg.Append(Localize.From("Inner exception: "));
				}
				msg.AppendFormat("{0} ({1})", ex.Message, ex.GetType().Name);
				if (inner.InnerException == null)
					break;
				inner = inner.InnerException;
			} while (true);
			msg.Append(lineSeparator);
			if (addStackTrace)
				msg.Append(ex.StackTrace);
			return msg.ToString();
		}

		public static string ToDetailedString(this Exception ex) { return ToDetailedString(ex, 3); }
		
		public static string ToDetailedString(this Exception ex, int maxInnerExceptions)
		{
			StringBuilder sb = new StringBuilder();
			try {
				for (;;)
				{
					sb.AppendFormat("{0}: {1}\n", ex.GetType().Name, ex.Message);
					AppendDataList(ex.Data, sb, "  ", " = ", "\n");
					sb.Append(ex.StackTrace);
					if ((ex = ex.InnerException) == null)
						break;
					sb.Append("\n\n");
					sb.Append(Localize.From("Inner exception:"));
					sb.Append(' ');
				}
			} catch { }
			return sb.ToString();
		}

		public static string DataList(this Exception ex)
		{
			return DataList(ex, "", " = ", "\n");
		}
		public static string DataList(this Exception ex, string linePrefix, string keyValueSeparator, string newLine)
		{
			return AppendDataList(ex.Data, null, linePrefix, keyValueSeparator, newLine).ToString();
		}

		public static StringBuilder AppendDataList(IDictionary dict, StringBuilder sb, string linePrefix, string keyValueSeparator, string newLine)
		{
			sb = sb ?? new StringBuilder();
			foreach (DictionaryEntry kvp in dict)
			{
				sb.Append(linePrefix);
				sb.Append(kvp.Key);
				sb.Append(keyValueSeparator);
				sb.Append(kvp.Value);
				sb.Append(newLine);
			}
			return sb;
		}
	}
}
