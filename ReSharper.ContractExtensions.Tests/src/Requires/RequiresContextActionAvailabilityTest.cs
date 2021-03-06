﻿using JetBrains.ReSharper.Intentions.CSharp.Test;
using NUnit.Framework;
using ReSharper.ContractExtensions.ContextActions;
using ReSharper.ContractExtensions.ContextActions.Requires;

namespace ReSharper.ContractExtensions.Tests.Preconditions
{
    [TestFixture]
    public class RequiresContextActionAvailabilityTest 
        : CSharpContextActionAvailabilityTestBase<AddRequiresContextAction>
    {
        protected override string ExtraPath
        {
            get { return "Requires"; }
        }

        
        [TestCase("AvailabilityDebug")]
        public void TestSimpleAvailability(string testSrc)
        {
            DoOneTest(testSrc);
        }

        [TestCase("AvailabilityIndexer")]
        public void TestIndexerAvailability(string testSrc)
        {
            DoOneTest(testSrc);
        }

        [TestCase("AvailabilityCornerCases")]
        [TestCase("AvailabilityOnPropertySetter")]
        public void Test_Corner_Cases(string testSrc)
        {
            DoOneTest(testSrc);
        }

        [TestCase("Availability")]
        [TestCase("AvailabilityFull")]
        [TestCase("AvailabilityOnAbstractClass")]
        [TestCase("AvailabilityOnInterface")]
        [TestCase("AvailabilityOnStaticClass")]
        public void TestFullAvailability(string testSrc)
        {
            DoOneTest(testSrc);
        }
    }
}