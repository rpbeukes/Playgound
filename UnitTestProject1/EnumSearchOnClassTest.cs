using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using Shouldly;
using System.Threading;

namespace EnumSearchOnClassUnitTests
{
    [TestClass]
    public class EnumSearchOnClassTest
    {
        [TestMethod]
        public void Get_enum_properties()
        {
            var dataClass = CreateTestData();

            var result = GetEnumProperties(dataClass)
                            .ToList();

            result.Count.ShouldBeGreaterThan(1);
            result.ElementAt(0).PropertyInfo.PropertyType.Name.ShouldBe(dataClass.Enum1Prop.GetType().Name);
            result.ElementAt(1).PropertyInfo.PropertyType.Name.ShouldBe(dataClass.ClassTwoProp.Enum2Prop.GetType().Name);
        }

        [TestMethod]
        public void Validate_enum_values()
        {
            var dataClass = CreateTestData();

            dataClass.Enum1Prop = (Enum1)99;
            dataClass.ClassTwoProp.Enum2Prop = (Enum2)100;

            var result = GetEnumProperties(dataClass)
                            .AsParallel()
                            .SelectMany(e => ValidateEnum(e))
                            .ToList()
                            ;

            result.Count.ShouldBeGreaterThan(1);
            result.ElementAt(0).ShouldContain(typeof(Enum1).Name);
            result.ElementAt(1).ShouldContain(typeof(Enum2).Name);
        }


        [TestMethod]
        public void Validate_derived_classes_with_invlaid_enum_values()
        {
            var dataClass = new ClassThree() { Enum2Prop = (Enum2)99 };

            var result = GetEnumProperties(dataClass)
                            .AsParallel()
                            .SelectMany(e => ValidateEnum(e))
                            .ToList()
                            ;

            result.Count.ShouldBe(1);
        }

        [TestMethod]
        public void Validate_derived_classes_with_no_vlaid_enum_values()
        {
            var dataClass = new ClassThree();

            var result = GetEnumProperties(dataClass)
                            .AsParallel()
                            .SelectMany(e => ValidateEnum(e))
                            .ToList()
                            ;

            result.Count.ShouldBe(0);
        }

        private IEnumerable<EnumData> GetEnumProperties(object classObj)
        {
            var filterOnClassesAndEnumProperties = classObj
                                                    .GetType()
                                                    .GetProperties()
                                                    .AsParallel()
                                                    .Where(p => (p.PropertyType.Name != "String" &&  //exclude string because it is reference type
                                                                 p.PropertyType.IsClass) || p.PropertyType.IsEnum)
                                                    ;

            foreach (var p in filterOnClassesAndEnumProperties)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", Thread.CurrentThread.ManagedThreadId, p.Name, p.PropertyType.Name));
                if (p.PropertyType.IsClass)
                {
                    foreach (var enumData in GetEnumProperties(p.GetValue(classObj)))
                        yield return enumData;
                }
                else
                    yield return new EnumData { PropertyInfo = p, EnumValue = (int)p.GetValue(classObj) };
            }
        }

        private IEnumerable<string> ValidateEnum(EnumData ed)
        {
            if (!Enum.IsDefined(ed.PropertyInfo.PropertyType, ed.EnumValue))
                yield return string.Format("Value not found for - {0}", ed.PropertyInfo.PropertyType.Name);
        }

        class EnumData
        {
            public PropertyInfo PropertyInfo { get; set; }
            public int EnumValue { get; set; }
        }

        private static ClassOne CreateTestData()
        {
            return new ClassOne()
            {
                ClassTwoProp = new ClassTwo() { Enum2Prop = Enum2.Three },
                Enum1Prop = Enum1.One
            };
        }
    }

    public class ClassOne
    {
        public int MyPropertyInt { get; set; }
        public Enum1 Enum1Prop { get; set; }
        public string MyPropertyString { get; set; }
        public ClassTwo ClassTwoProp { get; set; }
    }

    public class ClassTwo
    {
        public int MyPropertyInt { get; set; }
        public Enum2 Enum2Prop { get; set; }
        public string MyPropertyString { get; set; }
    }

    public class ClassThree : ClassTwo
    {
        public ClassThree()
        {
            Enum2Prop = Enum2.Four;
        }
    }

    public enum Enum1
    {
        One,
        Two
    }

    public enum Enum2
    {
        Three = 3,
        Four
    }
}
