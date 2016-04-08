/*
 * Created David on 7/20/2007 at 2:21 PM
 */

using System;
using System.Text;
using Loyc.Threading;

namespace Loyc
{
	/// <summary>
	/// Localize is a global hook into which a string-mapping localizer can be
	/// installed. It is designed to make internationalization exceptionally easy
	/// for developers. TODO: expand I18N features based on Mozilla's L20N.
	/// </summary><remarks>
	/// All Loyc code should call this hook in order to localize text (although
	/// as of June 2015, no one has made any translation tables). Use it like
	/// this:
	/// <code>
	/// string result = "Hello, {0}".Localized(userName);
	/// </code>
	/// If no localizer is installed, the format string is left unchanged.
	/// <para/>
	/// Whatever localizer is installed will look up the text in its database and
	/// return a translation. If no translation to the end user's language is
	/// available, an appropriate default translation should be returned: either the
	/// original text, or a translation to some default language, e.g. English.
	/// <para/>
	/// Alternately, assuming you have the ability to change the table of
	/// translations, you can use a Symbol in your code and call the other overload
	/// of Localize() to look up the text that should be shown to the end user:
	/// <code>
	/// string result = ((Symbol)"MY_STRING").Localized();
	/// string result = @@MY_STRING.Localized();  // Enhanced C# with #useSymbols
	/// </code>
	/// This is most useful for long strings or paragraphs of text, but I expect
	/// that some projects, as a policy, will use symbols for all localizable text.
	/// <p/>
	/// Localize.Formatter() is then called to make the completed string, unless the
	/// variable argument list is empty. It is possible to perform formatting
	/// separately, for example:
	/// <code>
	/// Console.WriteLine("{0} is {0:X} in hexadecimal".Localized(), N);
	/// </code>
	/// Here, writeline performs the formatting instead. However, Localize's
	/// default formatter, StringExt.FormatCore, has an extra feature that the 
	/// standard formatter does not: named arguments. Here is an example:
	/// <code>
	/// ...
	/// string verb = (IsFileLoaded ? "parse" : "load").Localized();
	/// MessageBox.Show(
	///     "Not enough memory to {load/parse} '{filename}'.".Localized(
	///       {Message}", "load/parse", verb, "filename", FileName));
	/// }
	/// </code>
	/// As you can see, named arguments are mentioned in the format string by
	/// specifying an argument name such as {filename} instead of a number like
	/// {0}. The variable argument list contains the same name followed by its
	/// value, e.g. "filename", FileName. This feature gives you, the developer,
	/// the opportunity to indicate to the translator person what a particular 
	/// argument is for.
	/// <p/>
	/// The translator must not change any of the arguments: the word "{filename}"
	/// is not to be translated.
	/// <p/>
	/// At run-time, the format string with named arguments is converted to a
	/// "normal" format string with numbered arguments. The above example would
	/// become "Could not {1} the file: {3}" and then be passed to string.Format.
	/// 
	/// <h3>Design rationale</h3>
	/// 
	/// Many developers don't want to spend time writing internationalization or
	/// localization code, and are tempted to write code that is only for one
	/// language. It's no wonder, because it's a relative pain in the neck.
	/// Microsoft suggests that code carry around a "ResourceManager" object and
	/// directly request strings from it:
	/// <code>
	/// private ResourceManager rm;
	/// rm = new ResourceManager("MyStrings", this.GetType().Assembly);
	/// Console.Writeline(rm.GetString("HEXER"), N);
	/// </code>
	/// This approach has drawbacks:
	/// * It may be cumbersome to pass around a ResourceManager instance between all
	///   classes that might contain localizable strings; a global facility is
	///   much more convenient.
	/// * The programmer has to put all translations in the resource file;
	///   consequently, writing the code is bothersome because the programmer has
	///   to switch to the resource file and add the string to it. Someone reading
	///   the code, in turn, can't tell what the string says and has to load up
	///   the resource file to find out.
	/// * It is nontrivial to change the localization manager; for instance, what if
	///   someone wants to store translations in an .ini, .xml or .les file rather 
	///   than inside the assembly? What if the user wants to centralize all
	///   translations for a set of assemblies, rather than having separate
	///   resources in each assembly? 
	/// * Keeping in mind that the guy in charge of translation is typically
	///   different than the guys writing most of the code, it makes sense to keep
	///   translations separate from everything else.
	/// <p/>
	/// The idea of the Localize facility is to convince programmers to support
	/// localization by making it dead-easy to do. By default it is not connected to
	/// any translator (it just passes strings through), so people who are only
	/// writing a program for a one-language market can easily make their code
	/// "multiligual-ready" without doing any extra work, since `.Localized()` is
	/// no harder to type than `string.Format()`.
	/// <p/>
	/// The translation system itself is separate, and connected to `Localized` by a
	/// delegate, for two reasons:
	/// <ol>
	/// <li>Multiple translation systems are possible. This class should be suitable
	///     for any .NET program, and some programs using this utility will want to
	///     plug-in a different localizer. </li>
	/// <li>I personally don't have the time or expertise to write a localizer at 
	///     this time. So until I do, the Localize class will make my code ready for 
	///     translation, although not actually localized.</li>
	/// </ol>
	/// In the open source world, most developers don't have a team of translators
	/// ready make translations for them. The idea of Loyc, for example, is that
	/// many different individuals--not one big team--of programmers will create
	/// and maintain features. By centralizing this translation facility, it should
	/// be straightforward for a single multilingual individual to translate the
	/// text of many modules made by many different people.
	/// <p/>
	/// To facilitate this, I propose that in addition to a translator, a program
	/// should be made to figure out all the strings/symbols for which translations 
	/// are needed. To do this it would scan source code (at compile time) for 
	/// calls to methods in this class and generate a list of strings and symbols 
	/// needing translation. It would also have to detect certain calls that
	/// perform translation implicity, such as IMessageSink.Write(). See
	/// <see cref="LocalizableAttribute"/>.
	/// </remarks>
	public static class Localize
	{
		public static ThreadLocalVariable<LocalizerDelegate> _localizer = new ThreadLocalVariable<LocalizerDelegate>(Passthru);
		public static ThreadLocalVariable<FormatterDelegate> _formatter = new ThreadLocalVariable<FormatterDelegate>(StringExt.FormatCore);

		/// <summary>Localizer method (thread-local)</summary>
		public static LocalizerDelegate Localizer
		{
			get { return _localizer.Value; }
			set { _localizer.Value = value; }
		}

		/// <summary>Formatting delegate (thread-local), which is set to 
		/// <see cref="StringExt.Format"/> by default.</summary>
		public static FormatterDelegate Formatter
		{
			get { return _formatter.Value; }
			set { _formatter.Value = value; }
		}

		/// <summary>
		/// This is the dummy translator, which is the default value of Localizer. 
		/// It passes strings through untranslated. A msgId symbol cannot be handled 
		/// so it is simply converted to a string.
		/// </summary>
		public static string Passthru(Symbol msgId, string msg)
		{
            return msg ?? (msgId == null ? null : msgId.Name);
		}

		#region Main Localize() methods

		/// <summary>
        /// This is the heart of the Localize class, which localizes and formats a
        /// string.
        /// </summary>
        /// <param name="resourceId">Resource ID used to look up a string. If
        /// it is null then message must be provided; otherwise, message is only used 
        /// if no translation is available for the specified ID.</param>
        /// <param name="message">The message to translate, which may include argument 
        /// placeholders (e.g. "{0}").</param>
        /// <param name="args">Arguments given to Formatter to fill in placeholders
        /// after the Localizer is called. If args is null or empty then Formatter
        /// is not called.</param>
        /// <returns>The translated and formatted string.</returns>
		public static string Localized(this Symbol resourceId, [Localizable] string message, params object[] args)
		{
			string localized = Localizer(resourceId, message);
			if (args == null || args.Length == 0)
				return localized;
			else
				return Formatter(localized, args);
		}
		public static string Localized(this Symbol resourceId, params object[] args)
			{ return Localized(resourceId, null, args); }
		public static string Localized([Localizable] this string message, params object[] args)
			{ return Localized(null, message, args); }

		#endregion

		#region Versions of Localize() specialized for 0, 1 or 2 arguments

		public static string Localized(this Symbol resourceId)
			{ return Localized(resourceId, null, null); }
		public static string Localized([Localizable] this string message)
			{ return Localized(null, message, null); }
		public static string Localized(this Symbol resourceId, [Localizable] string message)
			{ return Localized(resourceId, message, null); }

		static ScratchBuffer<object[]> _1arg = new ScratchBuffer<object[]>(() => new object[1]);
		static ScratchBuffer<object[]> _2args = new ScratchBuffer<object[]>(() => new object[2]);

		public static string Localized(this Symbol resourceId, object arg1)
		{
			object[] buf = _1arg.Value;
			buf[0] = arg1;
			var result = Localized(resourceId, null, buf);
			buf[0] = null;
			return result;
		}
		public static string Localized([Localizable] this string message, object arg1)
		{
			object[] buf = _1arg.Value;
			buf[0] = arg1;
			var result = Localized(null, message, buf); 
			buf[0] = null;
			return result;
		}
		public static string Localized(this Symbol resourceId, [Localizable] string message, object arg1)
		{
			object[] buf = _1arg.Value;
			buf[0] = arg1;
			var result = Localized(resourceId, message, buf); 
			buf[0] = null;
			return result;
		}

		public static string Localized(this Symbol resourceId, object arg1, object arg2)
		{
			object[] buf = _2args.Value;
			buf[0] = arg1;
			buf[1] = arg2;
			var result = Localized(resourceId, null, buf);
			buf[0] = buf[1] = null;
			return result;
		}
		public static string Localized([Localizable] this string message, object arg1, object arg2)
		{
			object[] buf = _2args.Value;
			buf[0] = arg1;
			buf[1] = arg2;
			var result = Localized(null, message, buf); 
			buf[0] = buf[1] = null;
			return result;
		}
		public static string Localized(this Symbol resourceId, [Localizable] string message, object arg1, object arg2)
		{
			object[] buf = _2args.Value;
			buf[0] = arg1;
			buf[1] = arg2;
			var result = Localized(resourceId, message, buf);
			buf[0] = buf[1] = null;
			return result;
		}

		#endregion
	}

	/// <summary>
	/// I plan to use this attribute someday to gather all the localizable strings 
	/// in an application. This attribute should be applied to a string function 
	/// parameter if the method calls Localized() using that parameter as the 
	/// format string.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
	public class LocalizableAttribute : System.Attribute { }

	public delegate string FormatterDelegate(string format, params object[] args);
	public delegate string LocalizerDelegate(Symbol msgId, string msg);
}
