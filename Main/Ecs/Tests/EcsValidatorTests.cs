﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Ecs.Tests
{
	// TODO: more tests
	public class EcsValidatorTests : Assert
	{
		[Test]
		public void SanitizeIdentifierTests()
		{
			AreEqual("I_aposd",  EcsValidators.SanitizeIdentifier("I'd"));
			AreEqual("_123",     EcsValidators.SanitizeIdentifier("123"));
			AreEqual("_plus5",   EcsValidators.SanitizeIdentifier("+5" ));
			AreEqual("__empty__",EcsValidators.SanitizeIdentifier(""   ));
			AreEqual("_lt_gt",   EcsValidators.SanitizeIdentifier("<>"));
		}
	}
}
